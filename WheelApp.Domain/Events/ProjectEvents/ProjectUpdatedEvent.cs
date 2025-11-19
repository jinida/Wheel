using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.ProjectEvents
{
    /// <summary>
    /// Raised when a project is updated
    /// </summary>
    public class ProjectUpdatedEvent : IDomainEvent
    {
        public Project Project { get; }
        public Dictionary<string, object> Changes { get; }
        public DateTime OccurredOn { get; }

        public ProjectUpdatedEvent(Project project, Dictionary<string, object> changes)
        {
            Project = project;
            Changes = changes;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
