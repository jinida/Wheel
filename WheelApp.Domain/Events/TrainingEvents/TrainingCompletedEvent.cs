using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.TrainingEvents
{
    /// <summary>
    /// Raised when training completes successfully
    /// </summary>
    public class TrainingCompletedEvent : IDomainEvent
    {
        public Training Training { get; }
        public TimeSpan Duration { get; }
        public DateTime OccurredOn { get; }

        public TrainingCompletedEvent(Training training, TimeSpan duration)
        {
            Training = training;
            Duration = duration;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
