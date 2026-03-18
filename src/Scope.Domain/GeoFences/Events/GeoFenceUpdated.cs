using Scope.Domain.Abstractions;

namespace Scope.Domain.GeoFences.Events;

public record GeoFenceUpdated(
    string GeoFenceId,
    string Name,
    string GeometryJson
) : IDomainEvent;