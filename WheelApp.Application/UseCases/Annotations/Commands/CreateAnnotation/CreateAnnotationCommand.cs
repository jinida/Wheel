using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotation;

/// <summary>
/// Command to create a new annotation
/// </summary>
public record CreateAnnotationCommand : ICommand<Result<AnnotationDto>>
{
    public int ImageId { get; init; }
    public int ProjectId { get; init; }
    public int ClassId { get; init; }
    public string? Information { get; init; }
}
