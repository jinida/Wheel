using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Events.ProjectEvents
{
    /// <summary>
    /// Raised when a project's type is changed
    /// </summary>
    public class ProjectTypeChangedEvent : IDomainEvent
    {
        public Project Project { get; }
        public ProjectType OldType { get; }
        public ProjectType NewType { get; }
        public DateTime OccurredOn { get; }

        public ProjectTypeChangedEvent(Project project, ProjectType oldType, ProjectType newType)
        {
            Project = project;
            OldType = oldType;
            NewType = newType;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
