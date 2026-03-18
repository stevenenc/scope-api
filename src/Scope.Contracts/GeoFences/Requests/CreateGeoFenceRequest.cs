namespace Scope.Contracts.GeoFences.Requests;

public record CreateGeoFenceRequest(
    string GeoFenceId,
    string UserId,
    string Name,
    string GeometryJson
);