namespace WheelApp.Infrastructure.Storage;

/// <summary>
/// Configuration options for file storage
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Base directory path for file storage
    /// </summary>
    public string BasePath { get; set; } = "wwwroot/uploads";

    /// <summary>
    /// Maximum file size in bytes (default: 10MB)
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Allowed file extensions for upload
    /// </summary>
    public string[] AllowedExtensions { get; set; } =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".gif"
    };
}
