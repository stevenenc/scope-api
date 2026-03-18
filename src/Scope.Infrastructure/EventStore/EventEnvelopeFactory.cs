using Scope.Contracts.Events;
using Scope.Domain.Abstractions;

namespace Scope.Infrastructure.EventStore;

public static class EventEnvelopeFactory
{
    public static EventEnvelope Create(
        IDomainEvent domainEvent,
        long globalPosition,
        string streamId,
        int eventNumber,
        string correlationId,
        string? causationId,
        string idempotencyKey,
        string schemaVersion = "v1",
        object? metadata = null)
    {
        return new EventEnvelope(
            EventId: Guid.NewGuid(),
            GlobalPosition: globalPosition,
            StreamId: streamId,
            EventType: domainEvent.GetType().Name,
            EventNumber: eventNumber,
            OccurredAtUtc: DateTime.UtcNow,
            SchemaVersion: schemaVersion,
            CorrelationId: correlationId,
            CausationId: causationId,
            IdempotencyKey: idempotencyKey,
            Data: domainEvent,
            Metadata: metadata
        );
    }
}