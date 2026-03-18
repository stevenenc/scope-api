using Scope.Domain.Common;
using Scope.Domain.GeoFences.Events;

namespace Scope.Domain.GeoFences;

public class GeoFence : AggregateBase
{
    public string UserId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string GeometryJson { get; private set; } = default!;

    public static GeoFence Create(string geoFenceId, string userId, string name, string geometryJson)
    {
        if (string.IsNullOrWhiteSpace(geoFenceId))
            throw new ArgumentException("GeoFenceId is required.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(geometryJson))
            throw new ArgumentException("GeometryJson is required.");

        var aggregate = new GeoFence();
        aggregate.RaiseEvent(new GeoFenceCreated(geoFenceId, userId, name, geometryJson));
        return aggregate;
    }

    public void Update(string name, string geometryJson)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(geometryJson))
            throw new ArgumentException("GeometryJson is required.");

        RaiseEvent(new GeoFenceUpdated(Id, name, geometryJson));
    }

    public void Apply(GeoFenceCreated e)
    {
        Id = e.GeoFenceId;
        UserId = e.UserId;
        Name = e.Name;
        GeometryJson = e.GeometryJson;
    }

    public void Apply(GeoFenceUpdated e)
    {
        Name = e.Name;
        GeometryJson = e.GeometryJson;
    }
}