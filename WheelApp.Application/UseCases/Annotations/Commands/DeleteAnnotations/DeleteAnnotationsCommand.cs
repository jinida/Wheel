using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotations;

/// <summary>
/// Command to bulk delete multiple annotations
/// Provides better performance and transaction consistency than individual deletes
/// </summary>
public record DeleteAnnotationsCommand : ICommand<Result<int>>
{
    /// <summary>
    /// List of annotation IDs to delete
    /// </summary>
    public List<int> Ids { get; init; } = new();
}
