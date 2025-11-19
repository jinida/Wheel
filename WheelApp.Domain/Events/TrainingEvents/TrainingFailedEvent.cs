using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.TrainingEvents
{
    /// <summary>
    /// Raised when training fails
    /// </summary>
    public class TrainingFailedEvent : IDomainEvent
    {
        public Training Training { get; }
        public string Reason { get; }
        public DateTime OccurredOn { get; }

        public TrainingFailedEvent(Training training, string reason)
        {
            Training = training;
            Reason = reason;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
