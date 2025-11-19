using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotationsByImageIds;

/// <summary>
/// Command to delete all annotations for specified image IDs
/// Provides better performance than individual deletes
/// </summary>
public record DeleteAnnotationsByImageIdsCommand : ICommand<Result<int>>
{
    /// <summary>
    /// List of image IDs whose annotations should be deleted
    /// </summary>
    public List<int> ImageIds { get; init; } = new();
}
