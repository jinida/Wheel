using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Services;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Roles.Commands.RandomSplitRoles;

/// <summary>
/// Handles random splitting of images with annotations into Train/Validation/Test sets
/// Special handling for Anomaly Detection projects:
/// - Normal class: only Validation or Test
/// - Anomaly class: Training, Validation, and Test
/// </summary>
public class RandomSplitRolesCommandHandler : ICommandHandler<RandomSplitRolesCommand, Result<RandomSplitRolesResult>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IRoleSplitService _roleSplitService;
    private readonly ILogger<RandomSplitRolesCommandHandler> _logger;

    public RandomSplitRolesCommandHandler(
        IProjectRepository projectRepository,
        IImageRepository imageRepository,
        IRoleRepository roleRepository,
        IAnnotationRepository annotationRepository,
        IProjectClassRepository projectClassRepository,
        IRoleSplitService roleSplitService,
        ILogger<RandomSplitRolesCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _imageRepository = imageRepository;
        _roleRepository = roleRepository;
        _annotationRepository = annotationRepository;
        _projectClassRepository = projectClassRepository;
        _roleSplitService = roleSplitService;
        _logger = logger;
    }

    public async Task<Result<RandomSplitRolesResult>> Handle(RandomSplitRolesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting random split for Project {ProjectId} with ratios Train:{Train}, Val:{Val}, Test:{Test}",
                request.ProjectId, request.TrainRatio, request.ValidationRatio, request.TestRatio);

            // Get project to check type
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                return Result.Failure<RandomSplitRolesResult>($"Project with ID {request.ProjectId} not found");
            }

            // Get all existing roles for the project (all images have roles) - with tracking for updates
            var rolesList = await _roleRepository.GetByProjectIdTrackingAsync(request.ProjectId, cancellationToken);

            var result = new RandomSplitRolesResult
            {
                TotalCount = rolesList.Count,
                SkippedCount = 0
            };

            // Special handling for Anomaly Detection projects (Type = 3)
            if (project.Type.Value == ProjectType.AnomalyDetection.Value)
            {
                result = await HandleAnomalyDetectionSplit(
                    request, project, rolesList, result, cancellationToken);
            }
            else
            {
                result = await HandleNormalSplit(
                    request, rolesList, result, cancellationToken);
            }


            result.Message = $"Successfully split {result.TrainCount + result.ValidationCount + result.TestCount} images: " +
                           $"Train={result.TrainCount}, Validation={result.ValidationCount}, Test={result.TestCount}";

            _logger.LogInformation(result.Message);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing random split for Project {ProjectId}", request.ProjectId);
            return Result.Failure<RandomSplitRolesResult>($"An error occurred during random split: {ex.Message}");
        }
    }

    private async Task<RandomSplitRolesResult> HandleAnomalyDetectionSplit(
        RandomSplitRolesCommand request,
        Project project,
        List<Role> existingRoles,
        RandomSplitRolesResult result,
        CancellationToken cancellationToken)
    {
        // Get project classes to identify Normal vs Anomaly
        var projectClasses = await _projectClassRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        var normalClass = projectClasses.FirstOrDefault(c => c.ClassIdx == 0);
        var anomalyClass = projectClasses.FirstOrDefault(c => c.ClassIdx == 1);

        if (normalClass == null || anomalyClass == null)
        {
            _logger.LogWarning("Could not find Normal/Anomaly classes for Anomaly Detection project");
            // Fall back to normal split if classes not found
            return await HandleNormalSplit(request, existingRoles, result, cancellationToken);
        }

        // Get annotations to classify images
        var annotations = await _annotationRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        var annotationsByImage = annotations.GroupBy(a => a.ImageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Separate images by their annotation class
        var normalImages = new List<int>();
        var anomalyImages = new List<int>();
        var unlabeledImages = new List<int>();

        foreach (var role in existingRoles)
        {
            if (annotationsByImage.TryGetValue(role.ImageId, out var imageAnnotations))
            {
                // Check the first annotation's class (assuming single class per image for Anomaly Detection)
                var firstAnnotation = imageAnnotations.FirstOrDefault();
                if (firstAnnotation?.ClassId == normalClass.Id)
                {
                    normalImages.Add(role.ImageId);
                }
                else if (firstAnnotation?.ClassId == anomalyClass.Id)
                {
                    anomalyImages.Add(role.ImageId);
                }
                else
                {
                    unlabeledImages.Add(role.ImageId);
                }
            }
            else
            {
                // No annotations - treat as unlabeled
                unlabeledImages.Add(role.ImageId);
            }
        }

        _logger.LogInformation("Found {NormalCount} Normal images and {AnomalyCount} Anomaly images",
            normalImages.Count, anomalyImages.Count);

        // Create a dictionary for quick role lookups
        var rolesByImageId = existingRoles.ToDictionary(r => r.ImageId);

        // Split Normal images (only Validation and Test, no Training)
        if (normalImages.Any())
        {
            // Normalize ratios for Normal class (no training)
            var normalValRatio = request.ValidationRatio / (request.ValidationRatio + request.TestRatio);
            var normalTestRatio = request.TestRatio / (request.ValidationRatio + request.TestRatio);

            var normalSplit = _roleSplitService.PerformRandomSplit(
                normalImages,
                0.0,  // No training for Normal class
                normalValRatio,
                normalTestRatio);

            // Update roles for Normal images (batch update)
            foreach (var kvp in normalSplit)
            {
                // Adjust role values since Normal can't be Training (0)
                var roleValue = kvp.Value == 0 ? 1 : kvp.Value; // Convert any Training to Validation

                if (rolesByImageId.TryGetValue(kvp.Key, out var role))
                {
                    role.ChangeRole(roleValue);
                    // No need to call UpdateAsync - entity is already tracked
                    UpdateResultCounts(roleValue, result, kvp.Key);
                }
            }
        }

        // Split Anomaly images (Training, Validation, and Test)
        if (anomalyImages.Any())
        {
            var anomalySplit = _roleSplitService.PerformRandomSplit(
                anomalyImages,
                request.TrainRatio,
                request.ValidationRatio,
                request.TestRatio);

            // Update roles for Anomaly images (batch update)
            foreach (var kvp in anomalySplit)
            {
                if (rolesByImageId.TryGetValue(kvp.Key, out var role))
                {
                    role.ChangeRole(kvp.Value);
                    // No need to call UpdateAsync - entity is already tracked
                    UpdateResultCounts(kvp.Value, result, kvp.Key);
                }
            }
        }

        // Save all changes at once (tracked entities will be updated)
        // The TransactionBehavior will handle the commit

        return result;
    }

    private async Task<RandomSplitRolesResult> HandleNormalSplit(
        RandomSplitRolesCommand request,
        List<Role> existingRoles,
        RandomSplitRolesResult result,
        CancellationToken cancellationToken)
    {
        // Get only roles for images with annotations
        var annotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        var imagesWithAnnotations = new HashSet<int>(annotations.Select(a => a.ImageId).Distinct());

        // Filter existing roles to only those with annotations and create dictionary for quick lookup
        var rolesByImageId = existingRoles
            .Where(r => imagesWithAnnotations.Contains(r.ImageId))
            .ToDictionary(r => r.ImageId);

        var imageIds = rolesByImageId.Keys.ToList();

        // Perform random split using the service
        var splitResult = _roleSplitService.PerformRandomSplit(
            imageIds,
            request.TrainRatio,
            request.ValidationRatio,
            request.TestRatio);

        // Update roles for each image
        foreach (var kvp in splitResult)
        {
            if (rolesByImageId.TryGetValue(kvp.Key, out var role))
            {
                role.ChangeRole(kvp.Value);
                // No need to call UpdateAsync - entity is already tracked
                UpdateResultCounts(kvp.Value, result, kvp.Key);
            }
        }
        return result;
    }

    private void UpdateResultCounts(
        int roleValue,
        RandomSplitRolesResult result,
        int imageId)
    {
        switch (roleValue)
        {
            case 0: // Train
                result.TrainCount++;
                result.UpdatedRoles[imageId] = "Train";
                break;
            case 1: // Validation
                result.ValidationCount++;
                result.UpdatedRoles[imageId] = "Validation";
                break;
            case 2: // Test
                result.TestCount++;
                result.UpdatedRoles[imageId] = "Test";
                break;
        }
    }

}