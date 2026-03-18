using System.Text.Json;
using MongoDB.Driver;
using Scope.Application.Abstractions;
using Scope.Contracts.Events;
using Scope.Domain.Abstractions;
using Scope.Infrastructure.Projections;

namespace Scope.Infrastructure.EventStore;

public class MongoEventStore : IEventStore
{
    private readonly IMongoCollection<EventDocument> _eventsCollection;

    public MongoEventStore(MongoDbSettings settings, GeoFenceProjector projector)
    {
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _eventsCollection = database.GetCollection<EventDocument>(settings.EventsCollectionName);

        CreateIndexes();
    }

    public async Task AppendEventsAsync(
        string streamId,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        string correlationId,
        string? causationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("StreamId is required.", nameof(streamId));

        if (events == null || events.Count == 0)
            return;

        var existingIdempotentEvent = await _eventsCollection
            .Find(x => x.IdempotencyKey == idempotencyKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingIdempotentEvent is not null)
            return;

        var latestEvent = await _eventsCollection
            .Find(x => x.StreamId == streamId)
            .SortByDescending(x => x.EventNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var currentVersion = latestEvent?.EventNumber ?? 0;

        if (currentVersion != expectedVersion)
        {
            throw new EventStoreConcurrencyException(
                $"Concurrency conflict for stream '{streamId}'. Expected version {expectedVersion}, but actual version is {currentVersion}.");
        }

        var eventDocuments = new List<EventDocument>();
        var envelopes = new List<EventEnvelope>();
        var nextEventNumber = currentVersion;
        
        var latestGlobalEvent = await _eventsCollection
            .Find(_ => true)
            .SortByDescending(x => x.GlobalPosition)
            .FirstOrDefaultAsync(cancellationToken);

        var nextGlobalPosition = latestGlobalEvent?.GlobalPosition ?? 0;

        foreach (var domainEvent in events)
        {
            nextEventNumber++;
            nextGlobalPosition++;

            var envelope = EventEnvelopeFactory.Create(
                domainEvent: domainEvent,
                globalPosition: nextGlobalPosition,
                streamId: streamId,
                eventNumber: nextEventNumber,
                correlationId: correlationId,
                causationId: causationId,
                idempotencyKey: idempotencyKey);

            envelopes.Add(envelope);

            eventDocuments.Add(new EventDocument
            {
                EventId = envelope.EventId,
                GlobalPosition = envelope.GlobalPosition,
                StreamId = envelope.StreamId,
                EventType = envelope.EventType,
                EventNumber = envelope.EventNumber,
                OccurredAtUtc = envelope.OccurredAtUtc,
                SchemaVersion = envelope.SchemaVersion,
                CorrelationId = envelope.CorrelationId,
                CausationId = envelope.CausationId,
                IdempotencyKey = envelope.IdempotencyKey,
                DataJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                MetadataJson = envelope.Metadata is null
                    ? null
                    : JsonSerializer.Serialize(envelope.Metadata)
            });
        }

        await _eventsCollection.InsertManyAsync(eventDocuments, cancellationToken: cancellationToken);
        Console.WriteLine($"[EventStore] Inserted {eventDocuments.Count} event(s) for stream {streamId}");
    }

    public async Task<IReadOnlyCollection<EventEnvelope>> ReadStreamAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("StreamId is required.", nameof(streamId));

        var documents = await _eventsCollection
            .Find(x => x.StreamId == streamId)
            .SortBy(x => x.EventNumber)
            .ToListAsync(cancellationToken);

        var envelopes = documents
            .Select(doc => new EventEnvelope(
                EventId: doc.EventId,
                GlobalPosition: doc.GlobalPosition,
                StreamId: doc.StreamId,
                EventType: doc.EventType,
                EventNumber: doc.EventNumber,
                OccurredAtUtc: doc.OccurredAtUtc,
                SchemaVersion: doc.SchemaVersion,
                CorrelationId: doc.CorrelationId,
                CausationId: doc.CausationId,
                IdempotencyKey: doc.IdempotencyKey,
                Data: doc.DataJson,
                Metadata: doc.MetadataJson
            ))
            .ToList();

        return envelopes;
    }

    private void CreateIndexes()
    {
        var streamAndEventNumberIndex = new CreateIndexModel<EventDocument>(
            Builders<EventDocument>.IndexKeys
                .Ascending(x => x.StreamId)
                .Ascending(x => x.EventNumber),
            new CreateIndexOptions { Unique = true });

        var idempotencyKeyIndex = new CreateIndexModel<EventDocument>(
            Builders<EventDocument>.IndexKeys
                .Ascending(x => x.IdempotencyKey),
            new CreateIndexOptions { Unique = true });

        var globalPositionIndex = new CreateIndexModel<EventDocument>(
            Builders<EventDocument>.IndexKeys
                .Ascending(x => x.GlobalPosition),
            new CreateIndexOptions { Unique = true });

        _eventsCollection.Indexes.CreateMany(new[]
        {
            streamAndEventNumberIndex,
            idempotencyKeyIndex,
            globalPositionIndex
        });
    }
}