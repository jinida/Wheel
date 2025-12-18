using WheelApp.Domain.Common;

namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Service interface for validating image files
/// </summary>
public interface IImageValidationService
{
    /// <summary>
    /// Validates an image file
    /// For non-seekable streams, pass a MemoryStream that can be reset
    /// </summary>
    Task<Result> ValidateAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the file extension is valid
    /// </summary>
    bool IsValidExtension(string extension);

    /// <summary>
    /// Checks if the file size is within limits
    /// </summary>
    bool IsValidSize(long size);

    /// <summary>
    /// Validates file content by checking magic bytes
    /// </summary>
    bool IsValidImageContent(byte[] header, string extension);
}
