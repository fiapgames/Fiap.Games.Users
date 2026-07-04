namespace User.Games.Fiap.Infrastructure.Auth;

public sealed class SystemCurrentUserService : ICurrentUserService
{
    public string? UserId => "system";
}
