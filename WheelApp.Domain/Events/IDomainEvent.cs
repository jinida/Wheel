using MediatR;

namespace WheelApp.Domain.Events
{
    /// <summary>
    /// Marker interface for domain events
    /// Must implement INotification for MediatR publishing
    /// </summary>
    public interface IDomainEvent : INotification
    {
        DateTime OccurredOn { get; }
    }
}
