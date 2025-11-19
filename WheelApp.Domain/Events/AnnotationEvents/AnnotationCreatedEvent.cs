using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.AnnotationEvents
{
    /// <summary>
    /// Raised when an annotation is created
    /// </summary>
    public class AnnotationCreatedEvent : IDomainEvent
    {
        public Annotation Annotation { get; }
        public DateTime OccurredOn { get; }

        public AnnotationCreatedEvent(Annotation annotation)
        {
            Annotation = annotation;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
