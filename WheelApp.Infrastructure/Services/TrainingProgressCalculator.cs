using WheelApp.Application.Common.Interfaces;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Services;

/// <summary>
/// Calculates training progress based on elapsed time
/// Progress is not stored in DB - calculated dynamically for real-time updates
/// </summary>
public class TrainingProgressCalculator : ITrainingProgressCalculator
{
    // Simulated training duration for demo purposes (can be configurable)
    private readonly TimeSpan _estimatedTrainingDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Calculates progress percentage based on training status and elapsed time
    /// </summary>
    public int CalculateProgress(Training training)
    {
        // Pending: 0%
        if (training.Status.Value == TrainingStatus.Pending.Value)
        {
            return 0;
        }

        // Completed: 100%
        if (training.Status.Value == TrainingStatus.Completed.Value)
        {
            return 100;
        }

        // Failed: Show last known progress (calculated based on when it failed)
        if (training.Status.Value == TrainingStatus.Failed.Value && training.EndedAt.HasValue)
        {
            var failedElapsed = training.EndedAt.Value - training.CreatedAt;
            var failedProgress = (int)((failedElapsed.TotalSeconds / _estimatedTrainingDuration.TotalSeconds) * 100);
            return Math.Min(failedProgress, 99); // Cap at 99% for failed trainings
        }

        // Running: Calculate based on elapsed time
        if (training.Status.Value == TrainingStatus.Running.Value)
        {
            var elapsed = DateTime.UtcNow - training.CreatedAt;
            var progress = (int)((elapsed.TotalSeconds / _estimatedTrainingDuration.TotalSeconds) * 100);

            // Cap at 99% until explicitly marked as complete
            return Math.Min(progress, 99);
        }

        return 0;
    }

    /// <summary>
    /// Gets estimated remaining time for running training
    /// </summary>
    public TimeSpan? GetEstimatedRemainingTime(Training training)
    {
        if (training.Status.Value != TrainingStatus.Running.Value)
        {
            return null;
        }

        var elapsed = DateTime.UtcNow - training.CreatedAt;
        var remaining = _estimatedTrainingDuration - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
