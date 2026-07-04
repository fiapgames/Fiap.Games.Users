namespace User.Games.Fiap.Application.Users;

public sealed record UserResponse(
    Guid Id,
    string Nome,
    string Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
