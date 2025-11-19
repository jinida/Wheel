using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WheelApp.Application.Common.Options;
using WheelApp.Domain.Common;

namespace WheelApp.Application.Services;

/// <summary>
/// Service for validating image files
/// </summary>
public class ImageValidationService
{
    private readonly ILogger<ImageValidationService> _logger;
    private readonly FileUploadOptions _options;

    private long MaxFileSize => _options.MaxFileSize;
    private string[] AllowedExtensions => _options.AllowedExtensions;

    // Magic bytes for common image formats
    private static readonly Dictionary<string, byte[][]> ImageSignatures = new()
    {
        { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".bmp", new[] { new byte[] { 0x42, 0x4D } } },
        { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } }
    };

    public ImageValidationService(ILogger<ImageValidationService> logger, IOptions<FileUploadOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Validates an image file
    /// For non-seekable streams, pass a MemoryStream that can be reset
    /// </summary>
    public async Task<Result> ValidateAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        // Check file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!IsValidExtension(extension))
        {
            return Result.Failure($"Invalid file extension. Allowed: {string.Join(", ", AllowedExtensions)}");
        }

        // Check file size
        if (!IsValidSize(stream.Length))
        {
            return Result.Failure($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB");
        }

        // Validate magic bytes (file header)
        var header = new byte[8];
        var bytesRead = await stream.ReadAsync(header, 0, header.Length, cancellationToken);

        if (bytesRead < header.Length)
        {
            return Result.Failure("File is too small or corrupted.");
        }

        // Reset stream position if possible (for MemoryStream or FileStream)
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (!IsValidImageContent(header, extension))
        {
            return Result.Failure("File content does not match the file extension. Possible corrupted or malicious file.");
        }

        return Result.Success();
    }

    /// <summary>
    /// Checks if the file extension is valid
    /// </summary>
    public bool IsValidExtension(string extension)
    {
        return AllowedExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if the file size is within limits
    /// </summary>
    public bool IsValidSize(long size)
    {
        return size > 0 && size <= MaxFileSize;
    }

    /// <summary>
    /// Validates file content by checking magic bytes
    /// </summary>
    public bool IsValidImageContent(byte[] header, string extension)
    {
        extension = extension.ToLowerInvariant();

        if (!ImageSignatures.ContainsKey(extension))
        {
            _logger.LogWarning("No signature found for extension: {Extension}", extension);
            return true; // If we don't have a signature, allow it
        }

        foreach (var signature in ImageSignatures[extension])
        {
            if (header.Take(signature.Length).SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }
}
