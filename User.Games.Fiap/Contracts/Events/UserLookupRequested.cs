namespace User.Games.Fiap.Contracts.Events;

public sealed record UserLookupRequested(
    Guid CorrelationId,
    Guid UserId,
    DateTimeOffset RequestedAt);
