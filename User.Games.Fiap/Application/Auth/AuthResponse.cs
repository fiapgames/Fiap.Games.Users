using User.Games.Fiap.Application.Users;

namespace User.Games.Fiap.Application.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UserResponse User);
