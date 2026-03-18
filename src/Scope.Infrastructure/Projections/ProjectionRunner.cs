using MongoDB.Driver;
using Scope.Infrastructure.EventStore;

namespace Scope.Infrastructure.Projections;

public class ProjectionRunner
{
    private readonly IMongoCollection<EventDocument> _eventsCollection;
    private readonly IMongoCollection<ProjectionCheckpoint> _checkpointCollection;
    private readonly GeoFenceProjector _geoFenceProjector;

    private const string ProjectorName = "GeoFenceProjector";

    public ProjectionRunner(MongoDbSettings settings, GeoFenceProjector geoFenceProjector)
    {
        _geoFenceProjector = geoFenceProjector;

        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);

        _eventsCollection = database.GetCollection<EventDocument>(settings.EventsCollectionName);
        _checkpointCollection = database.GetCollection<ProjectionCheckpoint>("projection_checkpoints");
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var checkpoint = await _checkpointCollection
            .Find(x => x.ProjectorName == ProjectorName)
            .FirstOrDefaultAsync(cancellationToken);

        var lastProcessed = checkpoint?.LastProcessedGlobalPosition ?? 0;

        var unprocessedEvents = await _eventsCollection
            .Find(x => x.GlobalPosition > lastProcessed)
            .SortBy(x => x.GlobalPosition)
            .ToListAsync(cancellationToken);

        if (unprocessedEvents.Count == 0)
            return;

        foreach (var doc in unprocessedEvents)
        {
            var envelope = new Scope.Contracts.Events.EventEnvelope(
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
            );

            await _geoFenceProjector.ProjectAsync(envelope);

            await _checkpointCollection.ReplaceOneAsync(
                x => x.ProjectorName == ProjectorName,
                new ProjectionCheckpoint
                {
                    ProjectorName = ProjectorName,
                    LastProcessedGlobalPosition = doc.GlobalPosition,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }
}