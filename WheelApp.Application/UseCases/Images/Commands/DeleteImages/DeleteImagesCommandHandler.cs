using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Images.Commands.DeleteImages;

public class DeleteImagesCommandHandler : ICommandHandler<DeleteImagesCommand, Result<DeleteImagesResultDto>>
{
    private readonly IImageRepository _imageRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<DeleteImagesCommandHandler> _logger;

    public DeleteImagesCommandHandler(
        IImageRepository imageRepository,
        IAnnotationRepository annotationRepository,
        IRoleRepository roleRepository,
        IFileStorage fileStorage,
        ILogger<DeleteImagesCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _annotationRepository = annotationRepository;
        _roleRepository = roleRepository;
        _fileStorage = fileStorage;        
        _logger = logger;
    }

    public async Task<Result<DeleteImagesResultDto>> Handle(
        DeleteImagesCommand request,
        CancellationToken cancellationToken)
    {
        var result = new DeleteImagesResultDto();

        try
        {
            _logger.LogInformation("Deleting {ImageCount} images", request.ImageIds.Count);

            // First, collect all valid images
            var imagesToDelete = new List<Domain.Entities.Image>();
            var annotationIdsToDelete = new List<int>();
            var roleIdsToDelete = new List<int>();

            // Bulk fetch all images at once
            var images = await _imageRepository.GetByIdsAsync(request.ImageIds, cancellationToken);
            var imageIdToImageMap = images.ToDictionary(img => img.Id);

            foreach (var imageId in request.ImageIds)
            {
                if (!imageIdToImageMap.TryGetValue(imageId, out var image))
                {
                    result.FailedCount++;
                    result.FailedNames.Add($"Image ID {imageId} not found");
                    _logger.LogWarning("Image ID {ImageId} not found", imageId);
                    continue;
                }

                imagesToDelete.Add(image);
            }

            // Bulk fetch all annotations and roles for the images being deleted
            if (imagesToDelete.Any())
            {
                var imageIds = imagesToDelete.Select(img => img.Id).ToList();

                // Bulk fetch annotations
                var annotations = await _annotationRepository.GetByImageIdsAsync(imageIds, cancellationToken);
                annotationIdsToDelete.AddRange(annotations.Select(a => a.Id));

                // Bulk fetch roles
                var roles = await _roleRepository.GetByImageIdsAsync(imageIds, cancellationToken);
                roleIdsToDelete.AddRange(roles.Select(r => r.Id));
            }

            // Bulk delete annotations
            if (annotationIdsToDelete.Any())
            {
                var annotationDeleteCount = await _annotationRepository.DeleteRangeAsync(annotationIdsToDelete, cancellationToken);
                _logger.LogInformation("Deleted {Count} annotations", annotationDeleteCount);
            }

            // Bulk delete roles
            if (roleIdsToDelete.Any())
            {
                var roleDeleteCount = await _roleRepository.BatchDeleteAsync(roleIdsToDelete, cancellationToken);
                _logger.LogInformation("Deleted {Count} roles", roleDeleteCount);
            }

            // Delete images and their files
            foreach (var image in imagesToDelete)
            {
                try
                {
                    // Delete file from storage
                    var deleteResult = await _fileStorage.DeleteAsync(image.Path, cancellationToken);
                    if (!deleteResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to delete file {Path}: {Error}", image.Path, deleteResult.Error);
                        // Continue with database deletion even if file deletion fails
                    }

                    // Delete image entity
                    await _imageRepository.DeleteAsync(image, cancellationToken);
                    result.DeletedCount++;

                    _logger.LogDebug("Successfully deleted image {ImageId}", image.Id);
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedNames.Add($"Image '{image.Name}': {ex.Message}");
                    _logger.LogError(ex, "Error deleting image {ImageId}", image.Id);
                }
            }

            // Changes are saved automatically by TransactionBehavior

            _logger.LogInformation("Delete complete: {DeletedCount} deleted, {FailedCount} failed",
                result.DeletedCount, result.FailedCount);

            result.Message = $"Deleted {result.DeletedCount} images";
            if (result.FailedCount > 0)
                result.Message += $", {result.FailedCount} failed";

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting images: {ErrorMessage}", ex.Message);
            return Result.Failure<DeleteImagesResultDto>($"Failed to delete images: {ex.Message}");
        }
    }
}
