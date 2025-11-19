using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications.ImageSpecifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Roles.Commands.UpdateRoleByIds;

/// <summary>
/// Handles bulk updating or creating roles for multiple images
/// Optimized for bulk operations with proper entity tracking management
/// </summary>
public class UpdateRoleByIdsCommandHandler : ICommandHandler<UpdateRoleByIdsCommand, Result<UpdateRoleByIdsResult>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly ILogger<UpdateRoleByIdsCommandHandler> _logger;

    public UpdateRoleByIdsCommandHandler(
        IRoleRepository roleRepository,
        IImageRepository imageRepository,
        IProjectRepository projectRepository,
        IProjectClassRepository projectClassRepository,
        IAnnotationRepository annotationRepository,
        ILogger<UpdateRoleByIdsCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _imageRepository = imageRepository;
        _projectRepository = projectRepository;
        _projectClassRepository = projectClassRepository;
        _annotationRepository = annotationRepository;
        _logger = logger;
    }

    public async Task<Result<UpdateRoleByIdsResult>> Handle(UpdateRoleByIdsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing bulk role update for {Count} images in Project {ProjectId}",
                request.ImageIds.Count, request.ProjectId);

            var result = new UpdateRoleByIdsResult();

            // Validate that images exist
            var imageSpec = new ImagesByIdsSpecification(request.ImageIds);
            var existingImages = await _imageRepository.FindAsync(imageSpec, cancellationToken);
            var existingImageIds = existingImages.Select(img => img.Id).ToHashSet();

            // Check for non-existent images
            var missingImageIds = request.ImageIds.Where(id => !existingImageIds.Contains(id)).ToList();
            if (missingImageIds.Any())
            {
                result.FailedImageIds.AddRange(missingImageIds);
                _logger.LogWarning("Images not found: {ImageIds}", string.Join(", ", missingImageIds));
            }

            // If no valid images found, return early
            if (!existingImageIds.Any())
            {
                return Result.Success(result);
            }

            // Validate RoleValue is in valid range (0-3) or null
            if (request.RoleValue.HasValue)
            {
                if (request.RoleValue.Value < 0 || request.RoleValue.Value > 3)
                {
                    return Result.Failure<UpdateRoleByIdsResult>($"Invalid role value: {request.RoleValue.Value}. Must be 0 (Train), 1 (Validation), 2 (Test), or 3 (None)");
                }
            }

            // Default to None (3) if no value provided
            int roleValue = request.RoleValue ?? 3;
            string roleTypeName = GetRoleTypeName(roleValue);

            // Check if this is an Anomaly Detection project
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                return Result.Failure<UpdateRoleByIdsResult>($"Project with ID {request.ProjectId} not found");
            }

            bool isAnomalyDetection = project.Type.Value == ProjectType.AnomalyDetection.Value;
            Dictionary<int, bool> imageIsNormalClass = new Dictionary<int, bool>();

            // If Anomaly Detection and trying to set Training role, check which images are Normal class
            if (isAnomalyDetection && roleValue == 0) // Training = 0
            {
                var projectClasses = await _projectClassRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
                var normalClass = projectClasses.FirstOrDefault(c => c.ClassIdx == 0);

                if (normalClass != null)
                {
                    // Get annotations to determine which images are Normal class
                    var annotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
                    var annotationsByImage = annotations.GroupBy(a => a.ImageId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var imageId in existingImageIds)
                    {
                        if (annotationsByImage.TryGetValue(imageId, out var imageAnnotations))
                        {
                            // Check if this image has Normal class annotations
                            imageIsNormalClass[imageId] = imageAnnotations.Any(a => a.ClassId == normalClass.Id);
                        }
                        else
                        {
                            imageIsNormalClass[imageId] = false;
                        }
                    }
                }
            }

            // Get all existing roles for this project WITH TRACKING
            var existingRoles = await _roleRepository.GetByProjectIdTrackingAsync(request.ProjectId, cancellationToken);

            // Create dictionaries for quick lookup
            var existingRolesByImageId = existingRoles
                .Where(r => existingImageIds.Contains(r.ImageId))
                .ToDictionary(r => r.ImageId);

            var rolesToCreate = new List<Role>();

            // Process each existing image
            foreach (var imageId in existingImageIds)
            {
                // Determine the actual role value to use
                int actualRoleValue = roleValue;

                // For Anomaly Detection: Normal class images cannot be Training (0)
                if (isAnomalyDetection && roleValue == 0 && imageIsNormalClass.ContainsKey(imageId) && imageIsNormalClass[imageId])
                {
                    actualRoleValue = 1; // Change to Validation instead
                    _logger.LogInformation("Image {ImageId} is Normal class in Anomaly Detection project, changing from Training to Validation", imageId);
                }

                string actualRoleTypeName = GetRoleTypeName(actualRoleValue);

                if (existingRolesByImageId.TryGetValue(imageId, out var existingRole))
                {
                    // Update existing role (already tracked)
                    existingRole.ChangeRole(actualRoleValue);
                    result.UpdatedRoles[imageId] = actualRoleTypeName;
                    result.UpdatedCount++;
                }
                else
                {
                    // Create new role
                    var newRole = Role.Create(imageId, request.ProjectId, actualRoleValue);
                    rolesToCreate.Add(newRole);
                    result.UpdatedRoles[imageId] = actualRoleTypeName;
                }
            }

            // Bulk create new roles
            if (rolesToCreate.Any())
            {
                await _roleRepository.AddRangeAsync(rolesToCreate, cancellationToken);
                result.CreatedCount = rolesToCreate.Count;
                _logger.LogInformation("Created {Count} new roles", result.CreatedCount);
            }

            // All tracked entities will be saved by TransactionBehavior
            _logger.LogInformation(
                "Bulk role update completed. Created: {Created}, Updated: {Updated}, Failed: {Failed}",
                result.CreatedCount, result.UpdatedCount, result.FailedImageIds.Count);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk role update for Project {ProjectId}", request.ProjectId);
            return Result.Failure<UpdateRoleByIdsResult>($"Failed to update roles: {ex.Message}");
        }
    }

    private string GetRoleTypeName(int roleValue)
    {
        return roleValue switch
        {
            0 => "Train",
            1 => "Validation",
            2 => "Test",
            3 => "None",
            _ => "None"
        };
    }
}