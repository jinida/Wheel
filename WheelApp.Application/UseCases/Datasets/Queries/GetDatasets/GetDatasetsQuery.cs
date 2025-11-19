using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Datasets.Queries.GetDatasets;

/// <summary>
/// Query to get paginated datasets with their image and project counts
/// </summary>
public record GetDatasetsQuery : IQuery<Result<PagedResult<DatasetDto>>>
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; } = 10;
}
