namespace Scope.Contracts.GeoFences.Requests;

public record UpdateGeoFenceRequest(
    string Name,
    string GeometryJson
);