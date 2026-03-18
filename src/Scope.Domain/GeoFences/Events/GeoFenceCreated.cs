using Scope.Domain.Abstractions;

namespace Scope.Domain.GeoFences.Events;

public record GeoFenceCreated(
    string GeoFenceId,
    string UserId,
    string Name,
    string GeometryJson
) : IDomainEvent;