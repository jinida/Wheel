using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Projects.Commands.DeleteProjects;

/// <summary>
/// Handles the bulk deletion of multiple projects with cascade delete
/// </summary>
public class DeleteProjectsCommandHandler : ICommandHandler<DeleteProjectsCommand, Result<int>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<DeleteProjectsCommandHandler> _logger;

    public DeleteProjectsCommandHandler(
        IProjectRepository projectRepository,
        ILogger<DeleteProjectsCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(DeleteProjectsCommand request, CancellationToken cancellationToken)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return Result.Failure<int>("No project IDs provided for deletion.");
        }

        int deletedCount = 0;
        var errors = new List<string>();

        foreach (var id in request.Ids)
        {
            try
            {
                // Get project info
                var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

                if (project == null)
                {
                    errors.Add($"Project with ID {id} not found.");
                    continue;
                }

                _logger.LogInformation("Deleting project '{ProjectName}' (ID: {ProjectId}) - database CASCADE DELETE will handle related entities",
                    project.Name.Value, id);

                var projectName = project.Name.Value; // Store for logging

                // Delete the project - database CASCADE DELETE will automatically delete:
                // - Roles (FK_Role_Project with ON DELETE CASCADE)
                // - Classes (FK_Class_Project with ON DELETE CASCADE)
                // - Annotations (FK_Annotation_Project with ON DELETE CASCADE)
                await _projectRepository.DeleteAsync(project, cancellationToken);

                deletedCount++;
                _logger.LogInformation("Successfully deleted project '{ProjectName}' (ID: {ProjectId}) with all related data via CASCADE DELETE",
                    projectName, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project with ID {ProjectId}", id);
                errors.Add($"Error deleting project {id}: {ex.Message}");
            }
        }

        // Commit all deletions at once
        if (deletedCount > 0)
        {
            // Changes are saved automatically by TransactionBehavior
        }

        if (errors.Any())
        {
            var errorMessage = $"Deleted {deletedCount} of {request.Ids.Count} project(s). Errors: {string.Join("; ", errors)}";
            return Result.Failure<int>(errorMessage);
        }

        return Result.Success(deletedCount);
    }
}
