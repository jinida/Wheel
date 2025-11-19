using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.TrainingEvents
{
    /// <summary>
    /// Raised when training starts
    /// </summary>
    public class TrainingStartedEvent : IDomainEvent
    {
        public Training Training { get; }
        public Project? Project { get; }
        public DateTime OccurredOn { get; }

        public TrainingStartedEvent(Training training, Project? project = null)
        {
            Training = training;
            Project = project;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
