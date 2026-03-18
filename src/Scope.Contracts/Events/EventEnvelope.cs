namespace Scope.Contracts.Events;

public record EventEnvelope(
    Guid EventId,
    long GlobalPosition,
    string StreamId,
    string EventType,
    int EventNumber,
    DateTime OccurredAtUtc,
    string SchemaVersion,
    string CorrelationId,
    string? CausationId,
    string IdempotencyKey,
    object Data,
    object? Metadata = null
);