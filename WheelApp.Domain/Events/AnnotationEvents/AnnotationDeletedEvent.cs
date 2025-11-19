namespace WheelApp.Domain.Events.AnnotationEvents
{
    /// <summary>
    /// Raised when an annotation is deleted
    /// </summary>
    public class AnnotationDeletedEvent : IDomainEvent
    {
        public int AnnotationId { get; }
        public int ImageId { get; }
        public int ProjectId { get; }
        public DateTime OccurredOn { get; }

        public AnnotationDeletedEvent(int annotationId, int imageId, int projectId)
        {
            AnnotationId = annotationId;
            ImageId = imageId;
            ProjectId = projectId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
