using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications.ImageSpecifications;
using WheelApp.Domain.Specifications.AnnotationSpecifications;
using WheelApp.Domain.Specifications.RoleSpecifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotationsByImageIds;

/// <summary>
/// Handles batch updating or creating annotations for multiple images
/// Optimized for bulk operations with transaction support
/// </summary>
public class UpdateAnnotationsByImageIdsCommandHandler : ICommandHandler<UpdateAnnotationsByImageIdsCommand, Result<UpdateAnnotationsByImageIdsResult>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<UpdateAnnotationsByImageIdsCommandHandler> _logger;

    public UpdateAnnotationsByImageIdsCommandHandler(
        IAnnotationRepository annotationRepository,
        IImageRepository imageRepository,
        IProjectClassRepository projectClassRepository,
        IProjectRepository projectRepository,
        IRoleRepository roleRepository,
        ILogger<UpdateAnnotationsByImageIdsCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _imageRepository = imageRepository;
        _projectClassRepository = projectClassRepository;
        _projectRepository = projectRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result<UpdateAnnotationsByImageIdsResult>> Handle(UpdateAnnotationsByImageIdsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing batch annotation update for {Count} images in Project {ProjectId}",
                request.ImageIds.Count, request.ProjectId);

            var result = new UpdateAnnotationsByImageIdsResult();

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

            // If no valid images, return early
            if (!existingImageIds.Any())
            {
                return Result.Success(result);
            }

            // Get existing annotations for the specified images WITH TRACKING (filtered at DB level)
            var annotationSpec = new AnnotationsByImageIdsSpecification(existingImageIds.ToList(), request.ProjectId);
            var existingAnnotations = await _annotationRepository.FindTrackingAsync(annotationSpec, cancellationToken);

            // Group existing annotations by ImageId for O(1) access
            var existingAnnotationsByImageId = existingAnnotations
                .GroupBy(a => a.ImageId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Handle ClassId null case - delete all annotations
            if (!request.ClassId.HasValue)
            {
                if (existingAnnotations.Any())
                {
                    var idsToDelete = existingAnnotations.Select(a => a.Id).ToList();
                    await _annotationRepository.DeleteRangeAsync(idsToDelete, cancellationToken);
                    result.RemovedCount = idsToDelete.Count;
                    _logger.LogInformation("Deleted {Count} annotations", result.RemovedCount);
                }
                return Result.Success(result);
            }

            // Validate ClassId exists
            var projectClass = await _projectClassRepository.GetByIdAsync(request.ClassId.Value, cancellationToken);
            if (projectClass == null)
            {
                return Result.Failure<UpdateAnnotationsByImageIdsResult>($"Class with ID {request.ClassId.Value} not found");
            }

            // Check if this is an Anomaly Detection project and Normal class
            var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project != null && project.Type.Value == ProjectType.AnomalyDetection.Value && projectClass.ClassIdx == 0)
            {
                // Get roles for the specified images to check if any are Training (filtered at DB level)
                var roleSpec = new RolesByImageIdsSpecification(existingImageIds.ToList(), request.ProjectId);
                var roles = await _roleRepository.FindAsync(roleSpec, cancellationToken);
                var trainingImageIds = roles
                    .Where(r => r.RoleType.Value == 0) // Training = 0
                    .Select(r => r.ImageId)
                    .ToList();

                if (trainingImageIds.Any())
                {
                    // Remove Training images from the list to process
                    existingImageIds.ExceptWith(trainingImageIds);
                    result.FailedImageIds.AddRange(trainingImageIds);

                    _logger.LogWarning(
                        "Cannot assign Normal class to Training role images in Anomaly Detection project. Failed images: {ImageIds}",
                        string.Join(", ", trainingImageIds));

                    // If no valid images left after filtering, return early
                    if (!existingImageIds.Any())
                    {
                        return Result.Success(result);
                    }
                }
            }

            var annotationsToCreate = new List<Annotation>();
            var annotationsForResult = new List<Annotation>();

            // Process each existing image
            foreach (var imageId in existingImageIds)
            {
                if (existingAnnotationsByImageId.TryGetValue(imageId, out var existingAnns))
                {
                    // Update existing annotations (already tracked)
                    foreach (var annotation in existingAnns)
                    {
                        annotation.ChangeClass(request.ClassId.Value);
                        annotationsForResult.Add(annotation);
                        result.UpdatedCount++;
                    }
                }
                else
                {
                    // Create new annotation
                    var newAnnotation = Annotation.Create(
                        imageId,
                        request.ProjectId,
                        request.ClassId.Value,
                        null // For classification tasks, Information is typically null
                    );
                    annotationsToCreate.Add(newAnnotation);
                    annotationsForResult.Add(newAnnotation);
                }
            }

            // Bulk create new annotations
            if (annotationsToCreate.Any())
            {
                await _annotationRepository.AddRangeAsync(annotationsToCreate, cancellationToken);
                result.CreatedCount = annotationsToCreate.Count;
                _logger.LogInformation("Created {Count} new annotations", result.CreatedCount);
            }

            var classDto = new ProjectClassDto
            {
                Id = projectClass.Id,
                ClassIdx = projectClass.ClassIdx != null ? projectClass.ClassIdx.Value : 0,
                Name = projectClass.Name ?? string.Empty,
                Color = projectClass.Color != null ? projectClass.Color.ToString() : string.Empty
            };

            result.Annotations = annotationsForResult.Select(annotation => new AnnotationDto
            {
                Id = annotation.Id,
                imageId = annotation.ImageId,
                Information = new List<Point2f>(), 
                classDto = classDto,
                CreatedAt = annotation.CreatedAt
            }).ToList();

            _logger.LogInformation(
                "Batch annotation update completed. Created: {Created}, Updated: {Updated}, Removed: {Removed}, Failed: {Failed}",
                result.CreatedCount, result.UpdatedCount, result.RemovedCount, result.FailedImageIds.Count);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch annotation update for Project {ProjectId}", request.ProjectId);
            return Result.Failure<UpdateAnnotationsByImageIdsResult>($"Failed to update annotations: {ex.Message}");
        }
    }
}