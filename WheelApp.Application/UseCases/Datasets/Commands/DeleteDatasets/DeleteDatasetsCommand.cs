using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Datasets.Commands.DeleteDatasets;

/// <summary>
/// Command to bulk delete multiple datasets
/// </summary>
public record DeleteDatasetsCommand : ICommand<Result<int>>
{
    public List<int> Ids { get; init; } = new();
}
