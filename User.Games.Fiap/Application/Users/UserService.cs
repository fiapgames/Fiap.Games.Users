using AutoMapper;
using AutoMapper.QueryableExtensions;
using FiapGames.Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using User.Games.Fiap.Application.Common;
using User.Games.Fiap.Infrastructure.Repositories;
using UserEntity = User.Games.Fiap.Domain.Entities.User;

namespace User.Games.Fiap.Application.Users;

public sealed class UserService(
    IUnitOfWork unitOfWork,
    IPasswordHasher<UserEntity> passwordHasher,
    IPublishEndpoint publishEndpoint,
    IMapper mapper) : IUserService
{
    private const int MaxPageSize = 100;

    public async Task<ServiceResult<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (await unitOfWork.Users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return ServiceResult<UserResponse>.Conflict("A user with this email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new UserEntity
        {
            Nome = request.Nome.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            CreatedAt = now,
            CreatedBy = "self-registration"
        };

        user.Password = passwordHasher.HashPassword(user, request.Password);

        await unitOfWork.Users.AddAsync(user, cancellationToken);

        await publishEndpoint.Publish(new UserCreatedEvent(
            user.Id,
            user.Nome,
            user.Email,
            user.CreatedAt.UtcDateTime), cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return ServiceResult<UserResponse>.Conflict("A user with this email already exists.");
        }

        return ServiceResult<UserResponse>.Created(mapper.Map<UserResponse>(user));
    }

    public async Task<ServiceResult<PagedResponse<UserResponse>>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = unitOfWork.Users.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user => user.Nome.Contains(term) || user.Email.Contains(term));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(user => user.Nome)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<UserResponse>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResponse<UserResponse>>.Ok(PagedResponse<UserResponse>.Create(users, page, pageSize, totalItems));
    }

    public async Task<ServiceResult<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.Query()
            .Where(user => user.Id == id)
            .ProjectTo<UserResponse>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? ServiceResult<UserResponse>.NotFound("User not found.")
            : ServiceResult<UserResponse>.Ok(user);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_Users_NormalizedEmail", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}
