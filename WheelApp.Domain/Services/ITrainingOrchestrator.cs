using WheelApp.Domain.Entities;
using WheelApp.Domain.Common;

namespace WheelApp.Domain.Services
{
    /// <summary>
    /// Domain service for orchestrating training operations
    /// </summary>
    public interface ITrainingOrchestrator
    {
        /// <summary>
        /// Validates if a project is ready for training
        /// </summary>
        Task<Result> ValidateReadyForTraining(Project project);

        /// <summary>
        /// Prepares a project for training (checks data splits, annotations, etc.)
        /// </summary>
        Task<Result<Training>> PrepareTraining(Project project);

        /// <summary>
        /// Determines if a training can be cancelled
        /// </summary>
        Task<bool> CanCancelTraining(Training training);

        /// <summary>
        /// Validates training completion requirements
        /// </summary>
        Task<Result> ValidateTrainingCompletion(Training training);
    }
}
