using Scope.Domain.Abstractions;

namespace Scope.Domain.Common;

public abstract class AggregateBase
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public string Id { get; protected set; } = string.Empty;
    public int Version { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        Apply(domainEvent);
        Version++;
        _uncommittedEvents.Add(domainEvent);
    }

    public void Replay(IEnumerable<IDomainEvent> history)
    {
        foreach (var domainEvent in history)
        {
            Apply(domainEvent);
            Version++;
        }
    }

    private void Apply(IDomainEvent domainEvent)
    {
        ((dynamic)this).Apply((dynamic)domainEvent);
    }
}