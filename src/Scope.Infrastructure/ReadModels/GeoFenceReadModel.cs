namespace Scope.Infrastructure.ReadModels;

public class GeoFenceReadModel
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string GeometryJson { get; set; } = default!;
    public DateTime UpdatedAtUtc { get; set; }
}