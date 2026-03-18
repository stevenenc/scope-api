using System.Text.Json;
using Scope.Application.Abstractions;
using Scope.Application.GeoFences.Commands;
using Scope.Contracts.Events;
using Scope.Domain.GeoFences;
using Scope.Domain.GeoFences.Events;
using Scope.Domain.Abstractions;

namespace Scope.Application.GeoFences.Handlers;

public class GeoFenceCommandHandler
{
    private readonly IEventStore _eventStore;

    public GeoFenceCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task Handle(CreateGeoFenceCommand command, CancellationToken cancellationToken = default)
    {
        var aggregate = GeoFence.Create(
            command.GeoFenceId,
            command.UserId,
            command.Name,
            command.GeometryJson
        );

        var events = aggregate.GetUncommittedEvents();

        await _eventStore.AppendEventsAsync(
            streamId: command.GeoFenceId,
            expectedVersion: 0,
            events: events,
            correlationId: command.CorrelationId,
            causationId: null,
            idempotencyKey: command.IdempotencyKey,
            cancellationToken: cancellationToken
        );

        aggregate.ClearUncommittedEvents();
    }

    public async Task Handle(UpdateGeoFenceCommand command, CancellationToken cancellationToken = default)
    {
        var stream = await _eventStore.ReadStreamAsync(command.GeoFenceId, cancellationToken);

        var domainEvents = stream
            .Select(e => DeserializeEvent(e))
            .ToList();

        var aggregate = new GeoFence();
        aggregate.Replay(domainEvents);

        var expectedVersion = aggregate.Version;

        aggregate.Update(command.Name, command.GeometryJson);

        var newEvents = aggregate.GetUncommittedEvents();

        await _eventStore.AppendEventsAsync(
            streamId: command.GeoFenceId,
            expectedVersion: expectedVersion,
            events: newEvents,
            correlationId: command.CorrelationId,
            causationId: null,
            idempotencyKey: command.IdempotencyKey,
            cancellationToken: cancellationToken
        );

        aggregate.ClearUncommittedEvents();
    }

    private IDomainEvent DeserializeEvent(EventEnvelope envelope)
    {
        return envelope.EventType switch
        {
            nameof(GeoFenceCreated) =>
                JsonSerializer.Deserialize<GeoFenceCreated>((string)envelope.Data)!,

            nameof(GeoFenceUpdated) =>
                JsonSerializer.Deserialize<GeoFenceUpdated>((string)envelope.Data)!,

            _ => throw new InvalidOperationException($"Unknown event type: {envelope.EventType}")
        };
    }
}