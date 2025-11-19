using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Images.Commands.UploadImages;

namespace WheelApp.Application.UseCases.Datasets.Commands.CreateDataset;

/// <summary>
/// Command to create a dataset and upload images in a single transaction
/// If upload fails, dataset creation is rolled back
/// </summary>
public class CreateDatasetCommand : ICommand<Result<CreateDatasetResultDto>>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<FileUploadInfo> Files { get; set; } = new();

    /// <summary>
    /// Progress callback to report upload progress
    /// </summary>
    public IProgress<UploadProgressInfo>? ProgressCallback { get; set; }
}

/// <summary>
/// Upload progress information
/// </summary>
public class UploadProgressInfo
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public string CurrentFileName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of dataset creation with images
/// </summary>
public class CreateDatasetResultDto
{
    public int DatasetId { get; set; }
    public string DatasetName { get; set; } = string.Empty;
    public int SuccessfulUploads { get; set; }
    public int FailedUploads { get; set; }
    public List<string> SuccessfulFiles { get; set; } = new();
    public List<string> FailedFiles { get; set; } = new();
    public List<string> ErrorMessages { get; set; } = new();
}
