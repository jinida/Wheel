namespace WheelApp.Application.UseCases.Images.Commands.UploadImages;

/// <summary>
/// File upload information for image uploads
/// Contains the stream to be saved by the Application layer
/// </summary>
public record FileUploadInfo
{
    public required string FileName { get; init; }
    public required Stream Stream { get; init; }
    public long FileSize { get; init; }
}
