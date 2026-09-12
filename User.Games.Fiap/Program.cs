using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using User.Games.Fiap.Application.Auth;
using User.Games.Fiap.Application.Mapping;
using User.Games.Fiap.Application.Users;
using User.Games.Fiap.Application.Validators;
using User.Games.Fiap.Configuration;
using User.Games.Fiap.Consumers;
using User.Games.Fiap.Infrastructure.Auth;
using User.Games.Fiap.Infrastructure.Data;
using User.Games.Fiap.Infrastructure.Notifications;
using User.Games.Fiap.Infrastructure.Repositories;
using UserEntity = User.Games.Fiap.Domain.Entities.User;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Games FIAP API",
        Version = "v1",
        Description = "Microsservico de usuarios, autenticacao e eventos."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe apenas o token JWT retornado no login."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    // The frontend (Vite dev server) calls this API directly, cross-origin, in dev.
    // Vite picks the next free port (5173, 5174, ...) when the default is busy, so any
    // localhost port is allowed here instead of a fixed one.
    options.AddPolicy(DevCorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host is "localhost" or "127.0.0.1")
            .AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddDataProtection()
    .SetApplicationName("User.Games.Fiap")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 20,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is missing.");

if (Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey must have at least 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is missing.");

    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});

builder.Services.AddMassTransit(bus =>
{
    bus.SetKebabCaseEndpointNameFormatter();
    bus.AddConsumer<UserLookupRequestedConsumer>();

    bus.AddEntityFrameworkOutbox<ApplicationDbContext>(outbox =>
    {
        outbox.QueryDelay = TimeSpan.FromSeconds(1);
        outbox.QueryMessageLimit = 100;
        outbox.UseSqlServer();
        outbox.UseBusOutbox();
    });

    bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
    {
        endpoint.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
        endpoint.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    });

    bus.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMq = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;

        cfg.Host(rabbitMq.HostName, rabbitMq.Port, rabbitMq.VirtualHost, host =>
        {
            host.Username(rabbitMq.UserName);
            host.Password(rabbitMq.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddAutoMapper(options => options.AddProfile<UserMappingProfile>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();

// Notificações agora são servidas pela Azure Function serverless, via HTTP.
builder.Services.AddHttpClient<INotificationsClient, NotificationsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Notifications:BaseUrl"]!.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);

    var functionKey = builder.Configuration["Notifications:FunctionKey"];
    if (!string.IsNullOrWhiteSpace(functionKey))
    {
        client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
    }
});

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await MigrateDatabaseAsync(app.Services);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Games FIAP API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await MigrateDatabaseAsync(app.Services);
}

app.UseRouting();
app.UseCors(DevCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

static async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
