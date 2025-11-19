using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Datasets.Commands.UpdateDataset;

/// <summary>
/// Command to update an existing dataset
/// </summary>
public record UpdateDatasetCommand : ICommand<Result<DatasetDto>>
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}
