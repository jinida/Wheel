using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.TrainingEvents
{
    /// <summary>
    /// Raised when training progress is updated
    /// </summary>
    public class TrainingProgressUpdatedEvent : IDomainEvent
    {
        public Training Training { get; }
        public int ProgressPercentage { get; }
        public DateTime OccurredOn { get; }

        public TrainingProgressUpdatedEvent(Training training, int progressPercentage)
        {
            Training = training;
            ProgressPercentage = progressPercentage;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
