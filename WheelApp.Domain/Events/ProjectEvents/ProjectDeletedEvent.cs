namespace WheelApp.Domain.Events.ProjectEvents
{
    /// <summary>
    /// Raised when a project is deleted
    /// </summary>
    public class ProjectDeletedEvent : IDomainEvent
    {
        public int ProjectId { get; }
        public string ProjectName { get; }
        public DateTime OccurredOn { get; }

        public ProjectDeletedEvent(int projectId, string projectName)
        {
            ProjectId = projectId;
            ProjectName = projectName;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
