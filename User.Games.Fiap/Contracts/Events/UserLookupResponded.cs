namespace User.Games.Fiap.Contracts.Events;

public sealed record UserLookupResponded(
    Guid CorrelationId,
    Guid UserId,
    bool Found,
    string? Nome,
    string? Email,
    DateTimeOffset OccurredAt);
