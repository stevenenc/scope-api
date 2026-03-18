using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Scope.Infrastructure.Projections;

public class ProjectionCheckpoint
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string ProjectorName { get; set; } = string.Empty;
    public long LastProcessedGlobalPosition { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}