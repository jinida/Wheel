using WheelApp.Domain.Common;
using WheelApp.Domain.Events.EvaluationEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// Evaluation entity storing evaluation results for a training run
    /// </summary>
    public class Evaluation : Entity
    {
        public int TrainingId { get; private set; }
        public FilePath Path { get; private set; }
        public string? MetricsJson { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Evaluation() { }  // For EF Core

        private Evaluation(int trainingId, FilePath path, string? metricsJson = null)
        {
            TrainingId = trainingId;
            Path = path;
            MetricsJson = metricsJson;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Factory method to create a new evaluation
        /// </summary>
        public static Evaluation Create(int trainingId, string path, string? metricsJson = null)
        {
            if (trainingId <= 0)
                throw new ValidationException(nameof(trainingId), "Training ID must be positive.");

            var filePath = FilePath.Create(path);
            var evaluation = new Evaluation(trainingId, filePath, metricsJson);
            evaluation.AddDomainEvent(new EvaluationCreatedEvent(evaluation));
            return evaluation;
        }

        /// <summary>
        /// Updates the metrics JSON
        /// </summary>
        public void UpdateMetrics(string? metricsJson)
        {
            var oldMetrics = MetricsJson;
            MetricsJson = metricsJson;
            AddDomainEvent(new EvaluationUpdatedEvent(this, oldMetrics, metricsJson));
        }
    }
}
