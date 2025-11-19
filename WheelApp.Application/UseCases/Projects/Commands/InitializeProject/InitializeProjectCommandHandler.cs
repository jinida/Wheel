using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Reflection;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Projects.Commands.InitializeProject;

/// <summary>
/// Handler for initializing a project by removing all annotations and roles
/// </summary>
public class InitializeProjectCommandHandler : ICommandHandler<InitializeProjectCommand, Result<InitializeProjectResult>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IDatasetRepository _datasetRepository;
    private readonly ILogger<InitializeProjectCommandHandler> _logger;

    public InitializeProjectCommandHandler(
        IProjectRepository projectRepository,
        IAnnotationRepository annotationRepository,
        IRoleRepository roleRepository,
        IImageRepository imageRepository,
        IDatasetRepository datasetRepository,
        ILogger<InitializeProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _annotationRepository = annotationRepository;
        _roleRepository = roleRepository;
        _imageRepository = imageRepository;
        _datasetRepository = datasetRepository;
        _logger = logger;
    }

    public async Task<Result<InitializeProjectResult>> Handle(InitializeProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing project {ProjectId}", request.ProjectId);

            // Check if project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result.Failure<InitializeProjectResult>($"Project with ID {request.ProjectId} not found");
            }

            // Get dataset to find all images
            var dataset = await _datasetRepository.GetByIdAsync(project.DatasetId, cancellationToken);
            if (dataset == null)
            {
                _logger.LogWarning("Dataset {DatasetId} not found for project {ProjectId}", project.DatasetId, request.ProjectId);
                return Result.Failure<InitializeProjectResult>($"Dataset not found for project");
            }

            // Get all images for the dataset
            var images = await _imageRepository.GetByDatasetIdAsync(dataset.Id, cancellationToken);
            var imageIds = images.Select(i => i.Id).ToList();

            if (!imageIds.Any())
            {
                _logger.LogInformation("No images found for project {ProjectId}", request.ProjectId);
                return Result.Success(new InitializeProjectResult
                {
                    Success = true,
                    Message = "No images to initialize",
                    ImagesAffected = 0,
                    AnnotationsDeleted = 0,
                    RolesDeleted = 0
                });
            }

            try
            {
                // Delete all annotations for this project
                var annotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
                var annotationIds = annotations.Select(a => a.Id).ToList();

                int annotationsDeleted = 0;
                if (annotationIds.Any())
                {
                    // Delete annotations in batches to avoid timeout
                    const int batchSize = 100;
                    for (int i = 0; i < annotationIds.Count; i += batchSize)
                    {
                        var batch = annotationIds.Skip(i).Take(batchSize).ToList();
                        foreach (var annotationId in batch)
                        {
                            var annotation = annotations.First(a => a.Id == annotationId);
                            await _annotationRepository.DeleteAsync(annotation, cancellationToken);
                            annotationsDeleted++;
                        }
                    }
                }

                var roles = await _roleRepository.GetByProjectIdTrackingAsync(request.ProjectId, cancellationToken);
                var roleIds = roles.Select(r => r.Id).ToList();

                int rolesDeleted = 0;
                if (roleIds.Any())
                {
                    // Delete roles in batches
                    const int batchSize = 100;
                    for (int i = 0; i < roleIds.Count; i += batchSize)
                    {
                        var batch = roleIds.Skip(i).Take(batchSize).ToList();
                        foreach (var roleId in batch)
                        {
                            var role = roles.First(r => r.Id == roleId);
                            role.ChangeRole(RoleType.None.Value);
                            rolesDeleted++;
                        }

                    }
                }

                _logger.LogInformation(
                    "Successfully initialized project {ProjectId}. Deleted {AnnotationsDeleted} annotations and {RolesDeleted} roles from {ImagesAffected} images",
                    request.ProjectId, annotationsDeleted, rolesDeleted, imageIds.Count);

                return Result.Success(new InitializeProjectResult
                {
                    Success = true,
                    AnnotationsDeleted = annotationsDeleted,
                    RolesDeleted = rolesDeleted,
                    ImagesAffected = imageIds.Count,
                    Message = $"Project initialized successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transaction for project {ProjectId}", request.ProjectId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize project {ProjectId}", request.ProjectId);
            return Result.Failure<InitializeProjectResult>($"Failed to initialize project: {ex.Message}");
        }
    }
}