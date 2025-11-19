namespace WheelApp.Application.Common.Options;

/// <summary>
/// Configuration options for file upload validation
/// </summary>
public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Allowed file extensions (e.g., ".jpg", ".png")
    /// </summary>
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

    /// <summary>
    /// Maximum file size in bytes (default: 20MB)
    /// </summary>
    public long MaxFileSize { get; set; } = 20 * 1024 * 1024;
}
