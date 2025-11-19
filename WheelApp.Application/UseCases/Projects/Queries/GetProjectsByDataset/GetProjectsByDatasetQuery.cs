using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Projects.Queries.GetProjectsByDataset;

/// <summary>
/// Query to get paginated projects for a specific dataset
/// </summary>
public record GetProjectsByDatasetQuery : IQuery<Result<PagedResult<ProjectDto>>>
{
    public int DatasetId { get; init; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; } = 10;
}
