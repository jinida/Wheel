using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotationsByImageIds;

/// <summary>
/// Command to create multiple annotations in batch by image IDs
/// Used for importing labels from previous image to multiple selected images
/// </summary>
public record CreateAnnotationsByImageIdsCommand : ICommand<Result<CreateAnnotationsByImageIdsResult>>
{
    /// <summary>
    /// List of annotation creation requests
    /// </summary>
    public List<AnnotationCreateRequest> Annotations { get; init; } = new();
}

/// <summary>
/// Single annotation creation request within a batch
/// </summary>
public record AnnotationCreateRequest
{
    public int ImageId { get; init; }
    public int ProjectId { get; init; }
    public int ClassId { get; init; }
    public List<Point2f>? Information { get; init; }
}

/// <summary>
/// Result of batch annotation creation
/// </summary>
public record CreateAnnotationsByImageIdsResult
{
    public int TotalCreated { get; init; }
    public List<AnnotationDto>? AnnotationDtos { get; init; }
}
