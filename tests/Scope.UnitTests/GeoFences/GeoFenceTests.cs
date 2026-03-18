using Scope.Domain.GeoFences;
using Scope.Domain.GeoFences.Events;

namespace Scope.UnitTests.GeoFences;

public class GeoFenceTests
{
    [Fact]
    public void Create_Should_Raise_GeoFenceCreated_Event_And_Set_State()
    {
        var geoFenceId = "gf-001";
        var userId = "user-001";
        var name = "Home";
        var geometryJson = "{\"type\":\"Polygon\",\"coordinates\":[]}";

        var aggregate = GeoFence.Create(geoFenceId, userId, name, geometryJson);

        Assert.Equal(geoFenceId, aggregate.Id);
        Assert.Equal(userId, aggregate.UserId);
        Assert.Equal(name, aggregate.Name);
        Assert.Equal(geometryJson, aggregate.GeometryJson);
        Assert.Equal(1, aggregate.Version);

        var events = aggregate.GetUncommittedEvents().ToList();
        Assert.Single(events);
        Assert.IsType<GeoFenceCreated>(events[0]);
    }

    [Fact]
    public void Update_Should_Raise_GeoFenceUpdated_Event_And_Update_State()
    {
        var aggregate = GeoFence.Create(
            "gf-001",
            "user-001",
            "Home",
            "{\"type\":\"Polygon\",\"coordinates\":[]}"
        );

        aggregate.ClearUncommittedEvents();

        var updatedName = "Work";
        var updatedGeometry = "{\"type\":\"Polygon\",\"coordinates\":[[1,2],[3,4]]}";

        aggregate.Update(updatedName, updatedGeometry);

        Assert.Equal(updatedName, aggregate.Name);
        Assert.Equal(updatedGeometry, aggregate.GeometryJson);
        Assert.Equal(2, aggregate.Version);

        var events = aggregate.GetUncommittedEvents().ToList();
        Assert.Single(events);
        Assert.IsType<GeoFenceUpdated>(events[0]);
    }

    [Fact]
    public void Replay_Should_Rebuild_Aggregate_State_From_History()
    {
        var history = new List<Scope.Domain.Abstractions.IDomainEvent>
        {
            new GeoFenceCreated(
                "gf-001",
                "user-001",
                "Home",
                "{\"type\":\"Polygon\",\"coordinates\":[]}"
            ),
            new GeoFenceUpdated(
                "gf-001",
                "Updated Home",
                "{\"type\":\"Polygon\",\"coordinates\":[[1,2],[3,4]]}"
            )
        };

        var aggregate = new GeoFence();
        aggregate.Replay(history);

        Assert.Equal("gf-001", aggregate.Id);
        Assert.Equal("user-001", aggregate.UserId);
        Assert.Equal("Updated Home", aggregate.Name);
        Assert.Equal("{\"type\":\"Polygon\",\"coordinates\":[[1,2],[3,4]]}", aggregate.GeometryJson);
        Assert.Equal(2, aggregate.Version);
    }
}