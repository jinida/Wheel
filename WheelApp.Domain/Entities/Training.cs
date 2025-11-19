using WheelApp.Domain.Common;
using WheelApp.Domain.Events.TrainingEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// Training entity representing a training run for a project
    /// </summary>
    public class Training : Entity
    {
        private readonly List<Evaluation> _evaluations = new();

        public int ProjectId { get; private set; }
        public TrainingName Name { get; private set; } = default!;
        public TrainingStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }

        // Navigation properties
        public Project? Project { get; private set; }  // EF Core navigation property
        public IReadOnlyCollection<Evaluation> Evaluations => _evaluations.AsReadOnly();

        private Training() { }  // For EF Core

        private Training(int projectId, string name)
        {
            ProjectId = projectId;
            Name = TrainingName.Create(name);
            Status = TrainingStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Factory method to start a new training
        /// </summary>
        public static Training Start(int projectId, string name)
        {
            if (projectId <= 0)
                throw new ValidationException(nameof(projectId), "Project ID must be positive.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException(nameof(name), "Training name is required.");

            return new Training(projectId, name);
        }

        /// <summary>
        /// Updates the training status with validation
        /// </summary>
        public void UpdateStatus(int newStatusValue)
        {
            var oldStatus = Status;
            var newStatus = TrainingStatus.FromValue(newStatusValue);

            if (!Status.CanTransitionTo(newStatus))
                throw new InvalidTrainingStatusException(Status.Value, newStatus.Value);

            Status = newStatus;

            // Raise events for all state transitions
            if (newStatus == TrainingStatus.Running)
            {
                AddDomainEvent(new TrainingStartedEvent(this));
            }
            else if (newStatus == TrainingStatus.Completed)
            {
                EndedAt = DateTime.UtcNow;
                var duration = EndedAt.Value - CreatedAt;
                AddDomainEvent(new TrainingCompletedEvent(this, duration));
            }
            else if (newStatus == TrainingStatus.Failed)
            {
                EndedAt = DateTime.UtcNow;
                AddDomainEvent(new TrainingFailedEvent(this, "Status transition to Failed"));
            }
        }

        /// <summary>
        /// Marks the training as completed
        /// </summary>
        public void Complete()
        {
            if (Status != TrainingStatus.Running)
                throw new InvalidTrainingStatusException(Status.Value, TrainingStatus.Completed.Value);

            Status = TrainingStatus.Completed;
            EndedAt = DateTime.UtcNow;

            var duration = EndedAt.Value - CreatedAt;
            AddDomainEvent(new TrainingCompletedEvent(this, duration));
        }

        /// <summary>
        /// Marks the training as failed
        /// </summary>
        public void Fail(string reason)
        {
            if (Status != TrainingStatus.Running)
                throw new InvalidTrainingStatusException(Status.Value, TrainingStatus.Failed.Value);

            Status = TrainingStatus.Failed;
            EndedAt = DateTime.UtcNow;

            AddDomainEvent(new TrainingFailedEvent(this, reason));
        }

        /// <summary>
        /// Adds an evaluation result to the training
        /// </summary>
        public void AddEvaluation(Evaluation evaluation)
        {
            if (evaluation == null)
                throw new ValidationException(nameof(evaluation), "Evaluation cannot be null.");

            _evaluations.Add(evaluation);
        }
    }
}
