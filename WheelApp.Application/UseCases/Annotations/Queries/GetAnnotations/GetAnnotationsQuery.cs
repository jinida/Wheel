using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Queries.GetAnnotations;

/// <summary>
/// Query to get annotations with filtering and paging
/// </summary>
public record GetAnnotationsQuery : IQuery<Result<PagedResult<AnnotationDto>>>
{
    public int? ProjectId { get; init; }
    public int? ImageId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
