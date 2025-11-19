using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Roles.Commands.RandomSplitRoles;

/// <summary>
/// Command to randomly split images in a project into Train/Validation/Test sets
/// For Anomaly Detection projects: only images with annotations are split, with special rules
/// For other project types: all images are split
/// </summary>
public class RandomSplitRolesCommand : ICommand<Result<RandomSplitRolesResult>>
{
    /// <summary>
    /// The project ID
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Ratio for training set (default 0.8)
    /// For Anomaly Detection: only Anomaly class images can be in training set
    /// </summary>
    public double TrainRatio { get; set; } = 0.8;

    /// <summary>
    /// Ratio for validation set (default 0.2)
    /// </summary>
    public double ValidationRatio { get; set; } = 0.2;

    /// <summary>
    /// Ratio for test set (default 0.0)
    /// </summary>
    public double TestRatio { get; set; } = 0.0;
}

/// <summary>
/// Result of the random split operation
/// </summary>
public class RandomSplitRolesResult
{
    /// <summary>
    /// Number of images assigned to training set
    /// </summary>
    public int TrainCount { get; set; }

    /// <summary>
    /// Number of images assigned to validation set
    /// </summary>
    public int ValidationCount { get; set; }

    /// <summary>
    /// Number of images assigned to test set
    /// </summary>
    public int TestCount { get; set; }

    /// <summary>
    /// Number of images skipped (no annotations)
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Total number of images processed
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Dictionary mapping ImageId to new RoleType name for UI updates
    /// </summary>
    public Dictionary<int, string> UpdatedRoles { get; set; } = new();
}