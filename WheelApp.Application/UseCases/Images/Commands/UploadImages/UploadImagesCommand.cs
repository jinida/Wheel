using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.UseCases.Datasets.Commands.CreateDataset;

namespace WheelApp.Application.UseCases.Images.Commands.UploadImages;

/// <summary>
/// Command to upload multiple images to an existing dataset
/// </summary>
public record UploadImagesCommand : ICommand<Result<UploadImagesResultDto>>
{
    public int DatasetId { get; init; }
    public int ProjectId { get; init; }
    public List<FileUploadInfo> Files { get; init; } = new();

    /// <summary>
    /// Progress callback to report upload progress
    /// </summary>
    public IProgress<UploadProgressInfo>? ProgressCallback { get; init; }
}

/// <summary>
/// Result of image upload operation
/// </summary>
public class UploadImagesResultDto
{
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> AddedNames { get; set; } = new();
    public List<string> SkippedNames { get; set; } = new();
    public List<string> FailedNames { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
