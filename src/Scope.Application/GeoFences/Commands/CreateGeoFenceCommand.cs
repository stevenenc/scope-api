namespace Scope.Application.GeoFences.Commands;

public record CreateGeoFenceCommand(
    string GeoFenceId,
    string UserId,
    string Name,
    string GeometryJson,
    string CorrelationId,
    string IdempotencyKey
);