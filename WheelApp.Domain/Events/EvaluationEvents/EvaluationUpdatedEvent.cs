using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.EvaluationEvents
{
    /// <summary>
    /// Raised when evaluation metrics are updated
    /// </summary>
    public class EvaluationUpdatedEvent : IDomainEvent
    {
        public Evaluation Evaluation { get; }
        public string? OldMetrics { get; }
        public string? NewMetrics { get; }
        public DateTime OccurredOn { get; }

        public EvaluationUpdatedEvent(Evaluation evaluation, string? oldMetrics, string? newMetrics)
        {
            Evaluation = evaluation;
            OldMetrics = oldMetrics;
            NewMetrics = newMetrics;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
