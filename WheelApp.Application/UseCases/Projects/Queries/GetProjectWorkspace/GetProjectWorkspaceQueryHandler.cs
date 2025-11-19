using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Projects.Queries.GetProjectWorkspace;

/// <summary>
/// Handles loading complete project workspace for labeling/annotation interface
/// Efficiently loads all data with minimal queries to prevent N+1 problems
/// </summary>
public class GetProjectWorkspaceQueryHandler : IQueryHandler<GetProjectWorkspaceQuery, Result<ProjectWorkspaceDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectClassRepository _classRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProjectWorkspaceQueryHandler> _logger;

    public GetProjectWorkspaceQueryHandler(
        IProjectRepository projectRepository,
        IProjectClassRepository classRepository,
        IImageRepository imageRepository,
        IRoleRepository roleRepository,
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        ILogger<GetProjectWorkspaceQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _classRepository = classRepository;
        _imageRepository = imageRepository;
        _roleRepository = roleRepository;
        _annotationRepository = annotationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ProjectWorkspaceDto>> Handle(
        GetProjectWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Loading workspace for Project {ProjectId}", request.ProjectId);

            // Step 1: Validate project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                return Result.Failure<ProjectWorkspaceDto>($"Project with ID {request.ProjectId} not found");
            }

            // Step 2: Load all data sequentially (DbContext is not thread-safe)
            // Note: While this is sequential, modern databases handle this efficiently
            var classes = await _classRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
            var images = await _imageRepository.GetByDatasetIdAsync(project.DatasetId, cancellationToken);
            var roles = await _roleRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
            var annotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

            // Step 3: Build lookup dictionaries for efficient nested mapping
            var rolesByImageId = roles.ToDictionary(r => r.ImageId);
            var annotationsByImageId = annotations
                .GroupBy(a => a.ImageId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create a dictionary for quick class lookup
            var classesDictionary = classes.ToDictionary(c => c.Id);

            // Step 4: Map entities to DTOs with nested structure
            var imageDtos = images.Select(img =>
            {
                // Get role for this image
                rolesByImageId.TryGetValue(img.Id, out var role);
                RoleTypeDto? roleTypeDto = role != null
                    ? new RoleTypeDto { Value = role.RoleType.Value, Name = role.RoleType.Name }
                    : null;

                // Get annotations for this image and map with class information
                annotationsByImageId.TryGetValue(img.Id, out var imageAnnotations);
                var annotationDtos = new List<AnnotationDto>();

                if (imageAnnotations != null)
                {
                    foreach (var annotation in imageAnnotations)
                    {
                        var annotationDto = new AnnotationDto
                        {
                            Id = annotation.Id,
                            imageId = annotation.ImageId,
                            Information = ParseInformationToPoints(annotation.Information),
                            CreatedAt = annotation.CreatedAt
                        };

                        // Add class information if available
                        if (classesDictionary.TryGetValue(annotation.ClassId, out var projectClass))
                        {
                            annotationDto.classDto = new ProjectClassDto
                            {
                                Id = projectClass.Id,
                                ClassIdx = projectClass.ClassIdx.Value,
                                Name = projectClass.Name,
                                Color = projectClass.Color.Value
                            };
                        }

                        annotationDtos.Add(annotationDto);
                    }
                }

                return new ImageDto
                {
                    Id = img.Id,
                    Name = img.Name,
                    Url = img.Path.Value,
                    CreatedAt = img.CreatedAt,
                    Annotation = annotationDtos,
                    RoleType = roleTypeDto
                };
            }).ToList();

            var workspace = new ProjectWorkspaceDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name.Value,
                DatasetId = project.DatasetId,
                ProjectType = project.Type.Value,
                Images = imageDtos,
                ProjectClasses = _mapper.Map<List<ProjectClassDto>>(classes),
                RoleTypes = GetRoleTypes(),
                ProjectTypes = GetProjectTypes()
            };

            _logger.LogInformation(
                "Workspace loaded: {ClassCount} classes, {ImageCount} images with nested annotations and roles",
                workspace.ProjectClasses.Count, workspace.Images.Count);

            return Result.Success(workspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project workspace for Project {ProjectId}: {ErrorMessage}",
                request.ProjectId, ex.Message);
            return Result.Failure<ProjectWorkspaceDto>($"Failed to load workspace: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all available role types
    /// </summary>
    private static List<RoleTypeDto> GetRoleTypes()
    {
        return new List<RoleTypeDto>
        {
            new() { Value = RoleType.Train.Value, Name = RoleType.Train.Name },
            new() { Value = RoleType.Validation.Value, Name = RoleType.Validation.Name },
            new() { Value = RoleType.Test.Value, Name = RoleType.Test.Name },
            new() { Value = RoleType.None.Value, Name = RoleType.None.Name }
        };
    }

    /// <summary>
    /// Gets all available project types
    /// </summary>
    private static List<ProjectTypeDto> GetProjectTypes()
    {
        return new List<ProjectTypeDto>
        {
            new() { Value = ProjectType.Classification.Value, Name = ProjectType.Classification.Name },
            new() { Value = ProjectType.ObjectDetection.Value, Name = ProjectType.ObjectDetection.Name },
            new() { Value = ProjectType.Segmentation.Value, Name = ProjectType.Segmentation.Name },
            new() { Value = ProjectType.AnomalyDetection.Value, Name = ProjectType.AnomalyDetection.Name }
        };
    }

    /// <summary>
    /// Parses JSON information string to List of Point2f
    /// Format: [[x1, y1], [x2, y2], ..., [xn, yn]]
    /// </summary>
    private static List<Point2f> ParseInformationToPoints(string? jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
            return new List<Point2f>();

        try
        {
            // Deserialize JSON array of coordinate pairs [[x1, y1], [x2, y2], ...]
            var coordPairs = JsonSerializer.Deserialize<float[][]>(jsonString);
            if (coordPairs == null || coordPairs.Length == 0)
                return new List<Point2f>();

            // Convert array of pairs to List<Point2f>
            var points = new List<Point2f>();
            foreach (var pair in coordPairs)
            {
                if (pair != null && pair.Length >= 2)
                {
                    points.Add(new Point2f(pair[0], pair[1]));
                }
            }
            return points;
        }
        catch
        {
            // Return empty list if JSON parsing fails
            return new List<Point2f>();
        }
    }
}
