using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotation;

/// <summary>
/// Command to update a single annotation
/// </summary>
public record UpdateAnnotationCommand : ICommand<Result>
{
    public int Id { get; init; }
    public int ClassId { get; init; }
    public List<Point2f>? Information { get; init; }
}
