using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Queries.GetAnnotationsByProject;

/// <summary>
/// Query to get annotations for a specific project
/// </summary>
public record GetAnnotationsByProjectQuery : IQuery<Result<List<AnnotationDto>>>
{
    public int ProjectId { get; init; }
}
