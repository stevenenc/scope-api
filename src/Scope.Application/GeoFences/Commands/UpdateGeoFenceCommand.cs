namespace Scope.Application.GeoFences.Commands;

public record UpdateGeoFenceCommand(
    string GeoFenceId,
    string Name,
    string GeometryJson,
    string CorrelationId,
    string IdempotencyKey
);