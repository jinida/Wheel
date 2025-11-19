using WheelApp.Domain.Entities;

namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Interface for calculating training progress based on elapsed time
/// </summary>
public interface ITrainingProgressCalculator
{
    /// <summary>
    /// Calculates progress percentage based on training status and elapsed time
    /// </summary>
    int CalculateProgress(Training training);

    /// <summary>
    /// Gets estimated remaining time for running training
    /// </summary>
    TimeSpan? GetEstimatedRemainingTime(Training training);
}
