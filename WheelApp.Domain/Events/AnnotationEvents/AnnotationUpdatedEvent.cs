using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.AnnotationEvents
{
    /// <summary>
    /// Raised when an annotation is updated
    /// </summary>
    public class AnnotationUpdatedEvent : IDomainEvent
    {
        public Annotation Annotation { get; }
        public string OldInformation { get; }
        public string NewInformation { get; }
        public DateTime OccurredOn { get; }

        public AnnotationUpdatedEvent(Annotation annotation, string oldInformation, string newInformation)
        {
            Annotation = annotation;
            OldInformation = oldInformation;
            NewInformation = newInformation;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
