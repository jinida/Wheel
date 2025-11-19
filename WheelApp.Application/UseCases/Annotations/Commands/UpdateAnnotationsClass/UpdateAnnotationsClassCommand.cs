using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotationsClass;

/// <summary>
/// Command to update the class of multiple annotations in batch
/// Provides better performance and transaction consistency than individual updates
/// </summary>
public record UpdateAnnotationsClassCommand : ICommand<Result<int>>
{
    /// <summary>
    /// List of annotation IDs to update
    /// </summary>
    public List<int> AnnotationIds { get; init; } = new();

    /// <summary>
    /// The new class ID to set for all annotations
    /// </summary>
    public int ClassId { get; init; }
}
