namespace Scope.Application.Abstractions;

public class EventStoreConcurrencyException : Exception
{
    public EventStoreConcurrencyException(string message) : base(message)
    {
    }
}