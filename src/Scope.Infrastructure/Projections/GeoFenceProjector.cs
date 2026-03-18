using MongoDB.Driver;
using Scope.Contracts.Events;
using Scope.Domain.GeoFences.Events;
using Scope.Infrastructure.ReadModels;
using System.Text.Json;
using Scope.Infrastructure.EventStore;

namespace Scope.Infrastructure.Projections;

public class GeoFenceProjector
{
    private readonly IMongoCollection<GeoFenceReadModel> _collection;

    public GeoFenceProjector(MongoDbSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);

        _collection = database.GetCollection<GeoFenceReadModel>("geofence_readmodels");
    }

    public async Task ProjectAsync(EventEnvelope envelope)
    {
        Console.WriteLine($"[Projector] Processing event: {envelope.EventType}");

        switch (envelope.EventType)
        {
            case nameof(GeoFenceCreated):
            {
                var evt = envelope.Data switch
                {
                    GeoFenceCreated created => created,
                    string json => JsonSerializer.Deserialize<GeoFenceCreated>(json)!,
                    _ => throw new InvalidOperationException($"Unsupported data type for {nameof(GeoFenceCreated)}: {envelope.Data.GetType().Name}")
                };

                var model = new GeoFenceReadModel
                {
                    Id = evt.GeoFenceId,
                    UserId = evt.UserId,
                    Name = evt.Name,
                    GeometryJson = evt.GeometryJson,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                await _collection.InsertOneAsync(model);
                Console.WriteLine($"[Projector] Inserted read model: {evt.GeoFenceId}");
                break;
            }

            case nameof(GeoFenceUpdated):
            {
                var evt = envelope.Data switch
                {
                    GeoFenceUpdated updated => updated,
                    string json => JsonSerializer.Deserialize<GeoFenceUpdated>(json)!,
                    _ => throw new InvalidOperationException($"Unsupported data type for {nameof(GeoFenceUpdated)}: {envelope.Data.GetType().Name}")
                };

                var update = Builders<GeoFenceReadModel>.Update
                    .Set(x => x.Name, evt.Name)
                    .Set(x => x.GeometryJson, evt.GeometryJson)
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                await _collection.UpdateOneAsync(
                    x => x.Id == evt.GeoFenceId,
                    update
                );

                Console.WriteLine($"[Projector] Updated read model: {evt.GeoFenceId}");
                break;
            }

            default:
                Console.WriteLine($"[Projector] Unknown event type: {envelope.EventType}");
                break;
        }
    }
}