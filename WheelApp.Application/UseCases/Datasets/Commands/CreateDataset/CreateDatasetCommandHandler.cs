using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.Services;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Datasets.Commands.CreateDataset;

/// <summary>
/// Handles dataset creation with images in a single transaction
/// Includes validation, sanitization, and batch insert to prevent N+1 queries
/// If anything fails (dataset creation OR image upload), everything is rolled back
/// </summary>
public class CreateDatasetCommandHandler : ICommandHandler<CreateDatasetCommand, Result<CreateDatasetResultDto>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ImageValidationService _validationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateDatasetCommandHandler> _logger;
    public CreateDatasetCommandHandler(
        IDatasetRepository datasetRepository,
        IImageRepository imageRepository,
        IFileStorage fileStorage,
        ImageValidationService validationService,
        IUnitOfWork unitOfWork,
        ILogger<CreateDatasetCommandHandler> logger)
    {
        _datasetRepository = datasetRepository;
        _imageRepository = imageRepository;
        _fileStorage = fileStorage;        
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CreateDatasetResultDto>> Handle(
        CreateDatasetCommand request,
        CancellationToken cancellationToken)
    {
        // NOTE: TransactionBehavior automatically wraps this in a transaction
        // No need to manually begin/commit transaction here

        try
        {
            // Step 1: Check for duplicate dataset name
            var existingDataset = await _datasetRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existingDataset != null)
            {
                _logger.LogWarning("Failed to create dataset. Dataset with name '{DatasetName}' already exists", request.Name);
                return Result.Failure<CreateDatasetResultDto>($"Dataset with name '{request.Name}' already exists.");
            }

            // Step 2: Create dataset
            _logger.LogInformation("Creating dataset '{DatasetName}' by {User}", request.Name, request.CreatedBy);

            var datasetName = DatasetName.Create(request.Name);
            var addingDataset = Dataset.Create(datasetName, request.Description, request.CreatedBy);

            var dataset = await _datasetRepository.AddAsync(addingDataset, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Dataset '{DatasetName}' created with ID {DatasetId}", request.Name, dataset.Id);

            // Step 3: Upload images if any
            var successfulFiles = new List<string>();
            var failedFiles = new List<string>();
            var errorMessages = new List<string>();

            if (request.Files.Any())
            {
                var folder = $"datasets/{dataset.Id}";
                int processedCount = 0;
                var imagesToAdd = new List<Image>();

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
                            errorMessages.Add($"{file.FileName}: {validationResult.Error}");
                            _logger.LogWarning("Validation failed for file {FileName}: {Error}", file.FileName, validationResult.Error);
                            processedCount++;
                            continue;
                        }

                        // Sanitize filename (remove invalid chars, add timestamp)
                        var sanitizedFileName = SanitizeFileName(file.FileName);

                        // Save file to storage
                        var saveResult = await _fileStorage.SaveAsync(
                            streamToValidate,
                            folder,
                            sanitizedFileName,
                            cancellationToken);

                        if (!saveResult.IsSuccess || saveResult.Value == null)
                        {
                            failedFiles.Add(file.FileName);
                            errorMessages.Add($"{file.FileName}: {saveResult.Error}");
                            _logger.LogWarning("Failed to save file {FileName}: {Error}", file.FileName, saveResult.Error);
                            processedCount++;
                            continue;
                        }

                        // Create image entity (collect for batch insert)
                        var image = Image.Create(file.FileName, "uploads/" + saveResult.Value, dataset.Id);
                        imagesToAdd.Add(image);

                        successfulFiles.Add(file.FileName);
                        processedCount++;

                        _logger.LogDebug("Successfully processed file {FileName}", file.FileName);
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add(file.FileName);
                        errorMessages.Add($"{file.FileName}: {ex.Message}");
                        _logger.LogError(ex, "Error processing file {FileName}", file.FileName);
                        processedCount++;
                    }
                    finally
                    {
                        memoryStream?.Dispose();
                    }
                }

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
            _logger.LogInformation("Successfully created dataset '{DatasetName}' with {ImageCount} images",
                request.Name, successfulFiles.Count);

            var result = new CreateDatasetResultDto
            {
                DatasetId = dataset.Id,
                DatasetName = dataset.Name.Value,
                SuccessfulUploads = successfulFiles.Count,
                FailedUploads = failedFiles.Count,
                SuccessfulFiles = successfulFiles,
                FailedFiles = failedFiles,
                ErrorMessages = errorMessages
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dataset with images: {ErrorMessage}", ex.Message);

            // TransactionBehavior will automatically rollback the transaction
            // Just return the failure result
            return Result.Failure<CreateDatasetResultDto>(
                $"Failed to create dataset: {ex.Message}");
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
}
