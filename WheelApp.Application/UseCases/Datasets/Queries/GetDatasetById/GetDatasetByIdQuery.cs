using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Datasets.Queries.GetDatasetById;

/// <summary>
/// Query to get a single dataset by ID
/// </summary>
public record GetDatasetByIdQuery : IQuery<Result<DatasetDto>>
{
    public int Id { get; init; }
}
