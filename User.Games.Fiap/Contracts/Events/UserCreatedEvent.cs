namespace User.Games.Fiap.Contracts.Events;

public sealed record UserCreatedEvent(
    Guid UserId,
    string Nome,
    string Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset OccurredAt);
