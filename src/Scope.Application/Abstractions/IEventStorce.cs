using Scope.Contracts.Events;
using Scope.Domain.Abstractions;

namespace Scope.Application.Abstractions;

public interface IEventStore
{
    Task AppendEventsAsync(
        string streamId,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        string correlationId,
        string? causationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventEnvelope>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default);
}