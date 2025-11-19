using Microsoft.Extensions.Logging;
using System.Linq;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.Services;
using WheelApp.Application.UseCases.Datasets.Commands.CreateDataset;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications.ImageSpecifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Images.Commands.UploadImages;

/// <summary>
/// Handles uploading multiple images to an existing dataset
/// Includes validation, sanitization, and batch insert to prevent N+1 queries
/// </summary>
public class UploadImagesCommandHandler : ICommandHandler<UploadImagesCommand, Result<UploadImagesResultDto>>
{
    private readonly IImageRepository _imageRepository;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ImageValidationService _validationService;
    private readonly ILogger<UploadImagesCommandHandler> _logger;

    public UploadImagesCommandHandler(
        IImageRepository imageRepository,
        IDatasetRepository datasetRepository,
        IFileStorage fileStorage,
        ImageValidationService validationService,
        ILogger<UploadImagesCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _datasetRepository = datasetRepository;
        _fileStorage = fileStorage;       
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<Result<UploadImagesResultDto>> Handle(
        UploadImagesCommand request,
        CancellationToken cancellationToken)
    {

        try
        {
            // Step 1: Verify dataset exists
            var dataset = await _datasetRepository.GetByIdAsync(request.DatasetId, cancellationToken);
            if (dataset == null)
            {
                _logger.LogWarning("Failed to upload images. Dataset with ID {DatasetId} not found", request.DatasetId);
                return Result.Failure<UploadImagesResultDto>($"Dataset with ID {request.DatasetId} not found.");
            }

            _logger.LogInformation("Uploading {FileCount} images to dataset {DatasetId}", request.Files.Count, request.DatasetId);

            // Step 2: Upload images
            var successfulFiles = new List<string>();
            var skippedFiles = new List<string>();
            var failedFiles = new List<string>();

            if (request.Files.Any())
            {
                var folder = $"datasets/{request.DatasetId}";
                int processedCount = 0;
                var imagesToAdd = new List<Image>();

                // Get existing images to check for duplicates
                var existingImagesSpec = new ImageByDatasetSpecification(request.DatasetId);
                var existingImages = await _imageRepository.FindAsync(existingImagesSpec, cancellationToken);
                var existingFileNames = new HashSet<FilePath>(
                    existingImages.Select(i => i.Path),
                    FilePathEqualityComparer.Instance);

                foreach (var file in request.Files)
                {
                    MemoryStream? memoryStream = null;
                    try
                    {
                        // Report progress
                        request.ProgressCallback?.Report(new UploadProgressInfo
                        {
                            TotalFiles = request.Files.Count,
                            ProcessedFiles = processedCount,
                            CurrentFileName = file.FileName,
                            Message = $"Processing {file.FileName}..."
                        });

                        // Sanitize filename first to get the actual path that will be saved
                        var sanitizedFileName = SanitizeFileName(file.FileName);
                        var expectedPath = FilePath.Create("uploads/" + folder + "/" + sanitizedFileName);

                        // Check for duplicate path
                        if (existingFileNames.Contains(expectedPath))
                        {
                            skippedFiles.Add(file.FileName);
                            _logger.LogDebug("Skipped duplicate file {FileName}", file.FileName);
                            processedCount++;
                            continue;
                        }

                        // For non-seekable streams (BrowserFileStream), copy to MemoryStream first
                        // This allows validation to read headers and reset position
                        Stream streamToValidate;
                        if (!file.Stream.CanSeek)
                        {
                            memoryStream = new MemoryStream();
                            await file.Stream.CopyToAsync(memoryStream, cancellationToken);
                            memoryStream.Position = 0;
                            streamToValidate = memoryStream;
                        }
                        else
                        {
                            streamToValidate = file.Stream;
                        }

                        // Validate file using ImageValidationService (magic bytes check)
                        var validationResult = await _validationService.ValidateAsync(
                            streamToValidate, file.FileName, cancellationToken);

                        if (validationResult.IsFailure)
                        {
                            failedFiles.Add(file.FileName);
                            _logger.LogWarning("Validation failed for file {FileName}: {Error}", file.FileName, validationResult.Error);
                            processedCount++;
                            continue;
                        }

                        // Save file to storage
                        var saveResult = await _fileStorage.SaveAsync(
                            streamToValidate,
                            folder,
                            sanitizedFileName,
                            cancellationToken);

                        if (!saveResult.IsSuccess || saveResult.Value == null)
                        {
                            failedFiles.Add(file.FileName);
                            _logger.LogWarning("Failed to save file {FileName}: {Error}", file.FileName, saveResult.Error);
                            processedCount++;
                            continue;
                        }

                        // Create image entity (collect for batch insert)
                        var image = Image.Create(file.FileName, "uploads/" + saveResult.Value, request.DatasetId);
                        imagesToAdd.Add(image);
                        existingFileNames.Add(image.Path); // Track for duplicate checking

                        successfulFiles.Add(file.FileName);
                        processedCount++;

                        _logger.LogDebug("Successfully processed file {FileName}", file.FileName);
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add(file.FileName);
                        _logger.LogError(ex, "Error processing file {FileName}", file.FileName);
                        processedCount++;
                    }
                    finally
                    {
                        memoryStream?.Dispose();
                    }
                }

                // Batch insert all images at once
                if (imagesToAdd.Any())
                {
                    await _imageRepository.AddRangeAsync(imagesToAdd, cancellationToken);
                    _logger.LogInformation("Batch inserted {ImageCount} images", imagesToAdd.Count);
                }

                // Report final progress
                request.ProgressCallback?.Report(new UploadProgressInfo
                {
                    TotalFiles = request.Files.Count,
                    ProcessedFiles = successfulFiles.Count,
                    CurrentFileName = "",
                    Message = "Upload complete!"
                });
            }

            // TransactionBehavior will commit the transaction automatically
            _logger.LogInformation("Successfully uploaded {SuccessCount} images to dataset {DatasetId}",
                successfulFiles.Count, request.DatasetId);

            var result = new UploadImagesResultDto
            {
                AddedCount = successfulFiles.Count,
                SkippedCount = skippedFiles.Count,
                FailedCount = failedFiles.Count,
                AddedNames = successfulFiles,
                SkippedNames = skippedFiles,
                FailedNames = failedFiles,
                Message = $"Successfully uploaded {successfulFiles.Count} images. " +
                          $"Skipped {skippedFiles.Count} duplicates. " +
                          $"Failed {failedFiles.Count} files."
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images: {ErrorMessage}", ex.Message);

            // TransactionBehavior will automatically rollback the transaction
            // Just return the failure result
            return Result.Failure<UploadImagesResultDto>(
                $"Failed to upload images: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitizes filename to prevent security issues
    /// Removes invalid characters and adds timestamp for uniqueness
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        name = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Add timestamp to ensure uniqueness
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"{name}_{timestamp}{extension}";
    }

    private class FilePathEqualityComparer : IEqualityComparer<FilePath>
    {
        public static readonly FilePathEqualityComparer Instance = new FilePathEqualityComparer();

        public bool Equals(FilePath? x, FilePath? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return StringComparer.OrdinalIgnoreCase.Equals(x.Value, y.Value);
        }

        public int GetHashCode(FilePath obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Value);
        }
    }
}
