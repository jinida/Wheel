using System.Text.Json;
using MediatR;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotation;
using WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotationsByImageIds;
using WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotationsByImageIds;
using WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotationsByImageIds;
using WheelApp.Application.UseCases.Roles.Commands.UpdateRoleByIds;
using WheelApp.Domain.Repositories;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for annotation CRUD operations and image labeling
    /// Phase 2 Refactoring - Combines annotation and labeling logic
    /// Refactored to use BaseProjectCoordinator for code reuse
    /// </summary>
    public class ProjectAnnotationCoordinator : BaseProjectCoordinator
    {
        private readonly ClassManagementService _classManagementService;
        private readonly ProjectWorkspaceCoordinator _workspaceCoordinator;
        private readonly IAnnotationRepository _annotationRepository;
        private readonly AnnotationService _annotationService;

        public ProjectAnnotationCoordinator(
            IMediator mediator,
            ProjectWorkspaceService workspaceService,
            ClassManagementService classManagementService,
            ProjectWorkspaceCoordinator workspaceCoordinator,
            IAnnotationRepository annotationRepository,
            AnnotationService annotationService)
            : base(mediator, workspaceService)
        {
            _classManagementService = classManagementService;
            _workspaceCoordinator = workspaceCoordinator;
            _annotationRepository = annotationRepository;
            _annotationService = annotationService;
        }

        #region Labeling Methods

        /// <summary>
        /// Sets label (class) for selected images
        /// Used for Classification and Anomaly Detection project types
        /// </summary>
        public async Task<Result<SetLabelResult>> SetLabelAsync(List<int> imageIds, ProjectClassDto? projectClass)
        {
            if (imageIds.Count == 0)
            {
                return Result.Failure<SetLabelResult>("No images selected");
            }

            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<SetLabelResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            // Skip if multi-label type (object detection/segmentation)
            var currentProjectType = context.Workspace.ProjectTypes.FirstOrDefault(pt => pt.Value == context.Workspace.ProjectType);
            if (currentProjectType?.MultiLabelType == true)
            {
                return Result.Failure<SetLabelResult>("Cannot set label for multi-label project types");
            }

            try
            {
                // Send batch update command
                var command = new UpdateAnnotationsByImageIdsCommand
                {
                    ImageIds = imageIds,
                    ProjectId = context.ProjectId,
                    ClassId = projectClass?.Id
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess && result.Value != null)
                {
                    // Create dictionaries for O(1) lookups
                    var annotationsByImageId = result.Value.Annotations?
                        .GroupBy(a => a.imageId)
                        .ToDictionary(g => g.Key, g => g.ToList())
                        ?? new Dictionary<int, List<AnnotationDto>>();

                    // Use base class helper to create image dictionary (eliminates duplication)
                    var imageDict = CreateImageDictionary(context.Workspace);

                    // Update UI efficiently using dictionary lookups - exclude failed images
                    var failedImageIds = new HashSet<int>(result.Value.FailedImageIds ?? new List<int>());

                    foreach (var imageId in imageIds)
                    {
                        // Skip failed images - they should keep their current annotation state
                        if (failedImageIds.Contains(imageId))
                            continue;

                        if (imageDict.TryGetValue(imageId, out var image))
                        {
                            // Set annotations from result or clear if none
                            image.Annotation = annotationsByImageId.TryGetValue(imageId, out var annotations)
                                ? annotations
                                : new List<AnnotationDto>();
                        }
                    }

                    // Check if some images failed (for Anomaly Detection: Training role images cannot have Normal class)
                    int failedCount = result.Value.FailedImageIds?.Count ?? 0;
                    int successCount = result.Value.CreatedCount + result.Value.UpdatedCount + result.Value.RemovedCount;

                    // Use base class helpers to check Anomaly Detection business rules (eliminates duplication)
                    bool isAnomalyDetection = IsAnomalyDetectionProject(context.Workspace);
                    bool isNormalClass = projectClass != null && IsNormalClass(context.Workspace, projectClass.Id);

                    return Result<SetLabelResult>.Success(new SetLabelResult
                    {
                        FailedCount = failedCount,
                        SuccessCount = successCount,
                        IsAnomalyDetection = isAnomalyDetection,
                        IsNormalClass = isNormalClass,
                        Message = "Label set successfully"
                    });
                }
                else
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    return Result.Failure<SetLabelResult>(GetErrorOrDefault(result.Error, "Failed to set label"));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<SetLabelResult>($"Error setting label: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets label by class index (for keyboard number keys)
        /// </summary>
        public async Task<Result> SetLabelByIndexAsync(List<int> imageIds, int classIndex)
        {
            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure(contextResult.Error!);
            }

            var context = contextResult.Value!;

            var orderedClasses = context.Workspace.ProjectClasses.OrderBy(c => c.Id).ToList();
            if (classIndex < 0 || classIndex >= orderedClasses.Count)
            {
                return Result.Failure($"Invalid class index: {classIndex}");
            }

            return await SetLabelAsync(imageIds, orderedClasses[classIndex]);
        }

        /// <summary>
        /// Imports annotations from previous image to selected images
        /// Uses the previously selected image from ImageSelectionService
        /// </summary>
        public async Task<Result<ImportPreviousLabelsResult>> ImportPreviousLabelsAsync(
            List<int> targetImageIds,
            ImageDto? previousImage,
            CancellationToken cancellationToken = default)
        {
            var result = new ImportPreviousLabelsResult();

            if (targetImageIds.Count == 0)
            {
                return Result.Failure<ImportPreviousLabelsResult>("No images selected");
            }

            // Check the previously selected image
            if (previousImage == null || previousImage.Annotation == null || !previousImage.Annotation.Any())
            {
                return Result.Failure<ImportPreviousLabelsResult>("Previous image has no annotations");
            }

            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<ImportPreviousLabelsResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                // Build batch annotation creation requests
                var annotationRequests = new List<AnnotationCreateRequest>();

                foreach (var targetImageId in targetImageIds)
                {
                    foreach (var sourceAnnotation in previousImage.Annotation)
                    {
                        if (sourceAnnotation.classDto == null) continue;

                        // Deep copy the Information list to avoid reference issues
                        var informationCopy = sourceAnnotation.Information
                            ?.Select(p => new Point2f(p.X, p.Y))
                            .ToList();

                        annotationRequests.Add(new AnnotationCreateRequest
                        {
                            ImageId = targetImageId,
                            ProjectId = context.ProjectId,
                            ClassId = sourceAnnotation.classDto.Id,
                            Information = informationCopy
                        });
                    }
                }

                if (annotationRequests.Any())
                {
                    var command = new CreateAnnotationsByImageIdsCommand
                    {
                        Annotations = annotationRequests
                    };

                    var createResult = await _mediator.Send(command);

                    if (createResult.IsSuccess)
                    {
                        var classDict = context.Workspace.ProjectClasses.ToDictionary(c => c.Id);
                        var annotationDtos = createResult.Value?.AnnotationDtos;
                        if (annotationDtos == null)
                        {
                            annotationDtos = new List<AnnotationDto>();
                        }

                        // Update local workspace
                        var imageDict = CreateImageDictionary(context.Workspace);
                        var annotationsByImageId = annotationDtos
                            .GroupBy(a => a.imageId)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        foreach (var imageId in targetImageIds)
                        {
                            if (imageDict.TryGetValue(imageId, out var image) &&
                                annotationsByImageId.TryGetValue(imageId, out var annotations))
                            {
                                // Replace entire annotation list (not AddRange) to avoid duplicates
                                // GetByImageIdsAsync returns ALL annotations for the image, not just new ones
                                image.Annotation.AddRange(annotations);
                            }
                        }

                        // Notify components that annotations were added to these images
                        _annotationService.NotifyImageAnnotationsUpdated(targetImageIds);

                        result.ImportedCount = createResult.Value.TotalCreated;
                        result.Message = $"Copied {createResult.Value.TotalCreated} labels to {targetImageIds.Count} images";
                        return Result<ImportPreviousLabelsResult>.Success(result);
                    }
                    else
                    {
                        // Use base class helper for error fallback (eliminates duplication)
                        return Result.Failure<ImportPreviousLabelsResult>(
                            GetErrorOrDefault(createResult.Error, "Failed to import annotations"));
                    }
                }
                else
                {
                    return Result.Failure<ImportPreviousLabelsResult>("No annotations to import");
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<ImportPreviousLabelsResult>($"Error importing labels: {ex.Message}");
            }
        }

        #endregion

        #region Annotation CRUD Methods

        /// <summary>
        /// Creates a bounding box annotation
        /// </summary>
        public async Task<Result> CreateBoundingBoxAsync(AnnotationDto annotation, int imageId)
        {
            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure(contextResult.Error!);
            }

            var context = contextResult.Value!;

            // Serialize Information to [[X1, Y1], [X2, Y2]] format
            var coordinates = annotation.Information
                .Select(p => new[] { p.X, p.Y })
                .ToList();
            var informationJson = JsonSerializer.Serialize(coordinates);

            var command = new CreateAnnotationCommand
            {
                ImageId = imageId,
                ProjectId = context.ProjectId,
                ClassId = annotation.classDto?.Id ?? 0,
                Information = informationJson
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value != null)
            {
                // Update the annotation ID with database-assigned ID
                annotation.Id = result.Value.Id;

                // Update local workspace with new annotation
                var imageDict = CreateImageDictionary(context.Workspace);

                if (imageDict.TryGetValue(imageId, out var image))
                {
                    // Add the new annotation to local image
                    image.Annotation ??= new List<AnnotationDto>();
                    image.Annotation.Add(annotation);
                }

                // Notify components that annotation was added
                _annotationService.NotifyImageAnnotationsUpdated(new List<int> { imageId });

                return Result.Success();
            }
            else
            {
                return Result.Failure(GetErrorOrDefault(result.Error, "Failed to save annotation"));
            }
        }

        /// <summary>
        /// Updates a bounding box annotation
        /// </summary>
        public async Task<Result> UpdateBoundingBoxAsync(AnnotationDto annotation)
        {
            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure(contextResult.Error!);
            }

            var context = contextResult.Value!;

            // Serialize Information to [[X1, Y1], [X2, Y2]] format
            var coordinates = annotation.Information
                .Select(p => new[] { p.X, p.Y })
                .ToList();
            var informationJson = JsonSerializer.Serialize(coordinates);

            var command = new Application.UseCases.Annotations.Commands.UpdateAnnotation.UpdateAnnotationCommand
            {
                Id = annotation.Id,
                ClassId = annotation.classDto?.Id ?? 0,
                Information = annotation.Information
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // Update local workspace
                var imageDict = CreateImageDictionary(context.Workspace);

                foreach (var image in context.Workspace.Images)
                {
                    if (image.Annotation != null)
                    {
                        var existingAnnotation = image.Annotation.FirstOrDefault(a => a.Id == annotation.Id);
                        if (existingAnnotation != null)
                        {
                            existingAnnotation.Information = annotation.Information;
                            existingAnnotation.classDto = annotation.classDto;
                            break;
                        }
                    }
                }

                return Result.Success();
            }
            else
            {
                return Result.Failure(GetErrorOrDefault(result.Error, "Failed to update annotation"));
            }
        }

        /// <summary>
        /// Creates a segmentation annotation
        /// </summary>
        public async Task<Result> CreateSegmentationAsync(AnnotationDto annotation, int imageId)
        {
            // Same implementation as BoundingBox - coordinates stored in [[X, Y], [X, Y]] format
            return await CreateBoundingBoxAsync(annotation, imageId);
        }

        /// <summary>
        /// Updates a segmentation annotation
        /// </summary>
        public async Task<Result> UpdateSegmentationAsync(AnnotationDto annotation)
        {
            // Same implementation as BoundingBox update
            return await UpdateBoundingBoxAsync(annotation);
        }

        /// <summary>
        /// Deletes a single annotation
        /// Note: Currently deletion is batch-based, not single
        /// </summary>
        public async Task<Result> DeleteAnnotationAsync(int annotationId)
        {
            // For now, delegate to batch delete with single ID
            return await DeleteAnnotationsBatchAsync(new List<int> { annotationId });
        }

        /// <summary>
        /// Deletes multiple annotations by their IDs
        /// </summary>
        public async Task<Result> DeleteAnnotationsBatchAsync(List<int> annotationIds)
        {
            if (annotationIds == null || !annotationIds.Any())
            {
                return Result.Success();
            }

            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                var command = new WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotations.DeleteAnnotationsCommand
                {
                    Ids = annotationIds
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    // Track which images were affected by the deletion
                    var affectedImageIds = new List<int>();

                    // Update local workspace - remove deleted annotations
                    foreach (var image in context.Workspace.Images)
                    {
                        if (image.Annotation != null)
                        {
                            var countBefore = image.Annotation.Count;
                            image.Annotation.RemoveAll(a => annotationIds.Contains(a.Id));

                            // Track if this image had annotations removed
                            if (image.Annotation.Count < countBefore)
                            {
                                affectedImageIds.Add(image.Id);
                            }
                        }
                    }

                    // Notify Project page that annotations were updated
                    if (affectedImageIds.Count > 0)
                    {
                        _annotationService.NotifyImageAnnotationsUpdated(affectedImageIds);
                    }

                    return Result.Success();
                }
                else
                {
                    return Result.Failure(GetErrorOrDefault(result.Error, "Failed to delete annotations"));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error deleting annotations: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all annotations and resets roles for selected images
        /// </summary>
        public async Task<Result<ClearAnnotationsResult>> ClearAllAnnotationsForImagesAsync(List<int> imageIds)
        {
            if (imageIds.Count == 0)
            {
                return Result.Failure<ClearAnnotationsResult>("No images selected");
            }

            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<ClearAnnotationsResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                // Delete all annotations for selected images
                var deleteAnnotationsCommand = new DeleteAnnotationsByImageIdsCommand
                {
                    ImageIds = imageIds
                };

                var deleteResult = await _mediator.Send(deleteAnnotationsCommand);

                if (!deleteResult.IsSuccess)
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    return Result.Failure<ClearAnnotationsResult>(GetErrorOrDefault(deleteResult.Error, "Failed to delete annotations"));
                }

                // Reset roles to None (3) for selected images
                var updateRoleCommand = new UpdateRoleByIdsCommand
                {
                    ProjectId = context.ProjectId,
                    ImageIds = imageIds,
                    RoleValue = 3 // None
                };

                var roleResult = await _mediator.Send(updateRoleCommand);

                if (!roleResult.IsSuccess)
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    return Result.Failure<ClearAnnotationsResult>(GetErrorOrDefault(roleResult.Error, "Failed to reset roles"));
                }

                // Update local workspace: clear annotations and reset roles
                var imageDict = CreateImageDictionary(context.Workspace);
                var noneRole = context.Workspace.RoleTypes.FirstOrDefault(r => r.Value == 3); // None role

                foreach (var imageId in imageIds)
                {
                    if (imageDict.TryGetValue(imageId, out var image))
                    {
                        // Clear annotations
                        image.Annotation = new List<AnnotationDto>();
                        // Reset role to None
                        image.RoleType = noneRole;
                    }
                }

                var result = new ClearAnnotationsResult
                {
                    DeletedAnnotationCount = deleteResult.Value,
                    ClearedImageCount = imageIds.Count,
                    Message = $"Cleared {deleteResult.Value} annotation(s) and reset roles for {imageIds.Count} image(s)"
                };

                // Notify components that annotations were cleared for these images
                _annotationService.NotifyImageAnnotationsUpdated(imageIds);

                return Result<ClearAnnotationsResult>.Success(result);
            }
            catch (Exception ex)
            {
                return Result.Failure<ClearAnnotationsResult>($"Error clearing annotations: {ex.Message}");
            }
        }

        #endregion
    }

    #region Result DTOs

    public class SetLabelResult
    {
        public int FailedCount { get; set; }
        public int SuccessCount { get; set; }
        public bool IsAnomalyDetection { get; set; }
        public bool IsNormalClass { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ImportPreviousLabelsResult
    {
        public int ImportedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ClearAnnotationsResult
    {
        public int DeletedAnnotationCount { get; set; }
        public int ClearedImageCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}
