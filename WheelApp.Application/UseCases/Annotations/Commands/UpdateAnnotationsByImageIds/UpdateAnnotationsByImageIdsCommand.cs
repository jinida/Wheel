using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotationsByImageIds;

/// <summary>
/// Command to update or set annotations for multiple images at once
/// Used for bulk operations in classification and anomaly detection tasks
/// </summary>
public record UpdateAnnotationsByImageIdsCommand : ICommand<Result<UpdateAnnotationsByImageIdsResult>>
{
    /// <summary>
    /// The list of image IDs to update annotations for
    /// </summary>
    public List<int> ImageIds { get; init; } = new();

    /// <summary>
    /// The project ID these annotations belong to
    /// </summary>
    public int ProjectId { get; init; }

    /// <summary>
    /// The class ID to set for all images. If null, clears all annotations
    /// </summary>
    public int? ClassId { get; init; }
}

/// <summary>
/// Result of batch annotation update operation
/// </summary>
public class UpdateAnnotationsByImageIdsResult
{
    /// <summary>
    /// Number of annotations successfully updated
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of annotations successfully created
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of annotations removed (when ClassId is null)
    /// </summary>
    public int RemovedCount { get; set; }

    /// <summary>
    /// List of image IDs that failed to process
    /// </summary>
    public List<int> FailedImageIds { get; set; } = new();

    /// <summary>
    /// List of updated/created annotations
    /// </summary>
    public List<AnnotationDto> Annotations { get; set; } = new();
}