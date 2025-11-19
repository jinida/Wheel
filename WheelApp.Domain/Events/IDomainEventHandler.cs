namespace WheelApp.Domain.Events
{
    /// <summary>
    /// Handler contract for domain events
    /// </summary>
    public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
    {
        Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
