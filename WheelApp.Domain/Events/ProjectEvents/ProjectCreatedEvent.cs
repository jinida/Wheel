using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.ProjectEvents
{
    /// <summary>
    /// Raised when a project is created
    /// </summary>
    public class ProjectCreatedEvent : IDomainEvent
    {
        public Project Project { get; }
        public DateTime OccurredOn { get; }

        public ProjectCreatedEvent(Project project)
        {
            Project = project;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
