using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Datasets.Commands.DeleteDatasets;

/// <summary>
/// Handles the bulk deletion of multiple datasets with cascade delete
/// </summary>
public class DeleteDatasetsCommandHandler : ICommandHandler<DeleteDatasetsCommand, Result<int>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<DeleteDatasetsCommandHandler> _logger;

    public DeleteDatasetsCommandHandler(
        IDatasetRepository datasetRepository,
        IProjectRepository projectRepository,
        ILogger<DeleteDatasetsCommandHandler> logger)
    {
        _datasetRepository = datasetRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(DeleteDatasetsCommand request, CancellationToken cancellationToken)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return Result.Failure<int>("No dataset IDs provided for deletion.");
        }

        int deletedCount = 0;
        var errors = new List<string>();

        foreach (var id in request.Ids)
        {
            try
            {
                // Get dataset info
                var dataset = await _datasetRepository.GetByIdAsync(id, cancellationToken);

                if (dataset == null)
                {
                    errors.Add($"Dataset with ID {id} not found.");
                    continue;
                }

                _logger.LogInformation("Deleting dataset '{DatasetName}' (ID: {DatasetId}) with cascade delete",
                    dataset.Name.Value, id);

                var datasetName = dataset.Name.Value; // Store for logging

                // Step 1: Delete all projects in this dataset
                // Projects CASCADE delete: Roles, Classes, Annotations, Trainings
                var projects = await _projectRepository.GetByDatasetIdAsync(id, cancellationToken);
                if (projects.Any())
                {
                    _logger.LogInformation("  - Deleting {ProjectCount} project(s)", projects.Count());
                    await _projectRepository.DeleteRangeAsync(projects, cancellationToken);
                }

                // Step 2: Delete the dataset
                // Dataset CASCADE deletes: Images (which cascade delete their Roles)
                _logger.LogInformation("  - Deleting dataset {DatasetId}", id);
                await _datasetRepository.DeleteAsync(dataset, cancellationToken);

                deletedCount++;
                _logger.LogInformation("Successfully deleted dataset '{DatasetName}' (ID: {DatasetId}) with all related data",
                    datasetName, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dataset with ID {DatasetId}", id);
                errors.Add($"Error deleting dataset {id}: {ex.Message}");
            }
        }

        // Commit all deletions at once
        if (deletedCount > 0)
        {
            // Changes are saved automatically by TransactionBehavior
        }

        if (errors.Any())
        {
            var errorMessage = $"Deleted {deletedCount} of {request.Ids.Count} dataset(s). Errors: {string.Join("; ", errors)}";
            return Result.Failure<int>(errorMessage);
        }

        return Result.Success(deletedCount);
    }
}
