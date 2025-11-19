using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Queries.GetAnnotationsByImage;

/// <summary>
/// Query to get annotations for a specific image
/// </summary>
public record GetAnnotationsByImageQuery : IQuery<Result<List<AnnotationDto>>>
{
    public int ImageId { get; init; }
}
