using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.EvaluationEvents
{
    /// <summary>
    /// Raised when an evaluation is created
    /// </summary>
    public class EvaluationCreatedEvent : IDomainEvent
    {
        public Evaluation Evaluation { get; }
        public DateTime OccurredOn { get; }

        public EvaluationCreatedEvent(Evaluation evaluation)
        {
            Evaluation = evaluation;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
