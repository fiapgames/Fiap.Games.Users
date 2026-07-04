namespace User.Games.Fiap.Infrastructure.Auth;

public interface ICurrentUserService
{
    string? UserId { get; }
}
