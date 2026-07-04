namespace User.Games.Fiap.Application.Users;

public sealed record CreateUserRequest(string Nome, string Email, string Password);
