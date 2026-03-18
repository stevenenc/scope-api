using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Scope.Infrastructure.EventStore;

public class EventDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid EventId { get; set; }

    public long GlobalPosition { get; set; }
    public string StreamId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int EventNumber { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string SchemaVersion { get; set; } = "v1";
    public string CorrelationId { get; set; } = string.Empty;
    public string? CausationId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}