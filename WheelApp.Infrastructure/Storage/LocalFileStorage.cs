using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Domain.Common;

namespace WheelApp.Infrastructure.Storage;

/// <summary>
/// Local file system implementation of IFileStorage
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly ILogger<LocalFileStorage> _logger;
    private readonly StorageOptions _options;
    private readonly string _basePath;

    public LocalFileStorage(
        ILogger<LocalFileStorage> logger,
        IOptions<StorageOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Normalize base path to absolute path
        _basePath = Path.IsPathRooted(_options.BasePath)
            ? _options.BasePath
            : Path.Combine(Directory.GetCurrentDirectory(), _options.BasePath);

        // Ensure base directory exists
        try
        {
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation("Created base storage directory at {BasePath}", _basePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create base storage directory at {BasePath}", _basePath);
            throw;
        }
    }

    /// <summary>
    /// Saves a file stream to local disk
    /// </summary>
    public async Task<Result<string>> SaveAsync(
        Stream stream,
        string folder,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (stream == null || !stream.CanRead)
            {
                return Result<string>.Failure("Invalid or unreadable stream");
            }

            if (string.IsNullOrWhiteSpace(folder))
            {
                return Result<string>.Failure("Folder cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Result<string>.Failure("File name cannot be empty");
            }

            // Sanitize folder and file name to prevent path traversal
            var sanitizedFolder = SanitizePath(folder);
            var sanitizedFileName = SanitizeFileName(fileName);

            if (sanitizedFolder == null)
            {
                return Result<string>.Failure("Invalid folder path");
            }

            if (sanitizedFileName == null)
            {
                return Result<string>.Failure("Invalid file name");
            }

            // Validate file extension
            var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
            if (!_options.AllowedExtensions.Contains(extension))
            {
                return Result<string>.Failure($"File extension {extension} is not allowed. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}");
            }

            // Validate file size
            if (stream.Length > _options.MaxFileSize)
            {
                var maxSizeMB = _options.MaxFileSize / (1024.0 * 1024.0);
                return Result<string>.Failure($"File size exceeds maximum allowed size of {maxSizeMB:F2} MB");
            }

            // Build full directory path
            var fullDirectoryPath = Path.Combine(_basePath, sanitizedFolder);

            // Create directory if it doesn't exist
            if (!Directory.Exists(fullDirectoryPath))
            {
                Directory.CreateDirectory(fullDirectoryPath);
                _logger.LogInformation("Created directory: {DirectoryPath}", fullDirectoryPath);
            }

            // Build full file path
            var fullFilePath = Path.Combine(fullDirectoryPath, sanitizedFileName);

            // Validate that the final path is still within base path (defense in depth)
            if (!IsPathSafe(fullFilePath))
            {
                return Result<string>.Failure("Invalid file path - potential path traversal detected");
            }

            // Save file to disk
            await using (var fileStream = new FileStream(
                fullFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920, // 80KB buffer for better performance
                useAsync: true))
            {
                // For seekable streams, reset to beginning
                if (stream.CanSeek && stream.Position != 0)
                {
                    stream.Position = 0;
                }

                // Copy entire stream to file
                await stream.CopyToAsync(fileStream, 81920, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            }

            // Return relative path from base path
            var relativePath = Path.Combine(sanitizedFolder, sanitizedFileName);

            _logger.LogInformation("Successfully saved file: {FilePath}", relativePath);

            return Result<string>.Success(relativePath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File save operation was cancelled");
            return Result<string>.Failure("Operation was cancelled");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while saving file: {FileName}", fileName);
            return Result<string>.Failure($"Failed to save file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while saving file: {FileName}", fileName);
            return Result<string>.Failure("Unauthorized access to file system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving file: {FileName}", fileName);
            return Result<string>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves a file as a stream
    /// </summary>
    public async Task<Result<Stream>> GetAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Result<Stream>.Failure("Path cannot be empty");
            }

            // Sanitize path
            var sanitizedPath = SanitizePath(path);
            if (sanitizedPath == null)
            {
                return Result<Stream>.Failure("Invalid file path");
            }

            // Build full path
            var fullPath = Path.Combine(_basePath, sanitizedPath);

            // Validate path safety
            if (!IsPathSafe(fullPath))
            {
                return Result<Stream>.Failure("Invalid file path - potential path traversal detected");
            }

            // Check if file exists
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FilePath}", path);
                return Result<Stream>.Failure("File not found");
            }

            // Open file stream
            var fileStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            _logger.LogInformation("Successfully opened file stream: {FilePath}", path);

            return await Task.FromResult(Result<Stream>.Success(fileStream));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File get operation was cancelled");
            return Result<Stream>.Failure("Operation was cancelled");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while reading file: {Path}", path);
            return Result<Stream>.Failure($"Failed to read file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while reading file: {Path}", path);
            return Result<Stream>.Failure("Unauthorized access to file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while reading file: {Path}", path);
            return Result<Stream>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    public async Task<Result> DeleteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Result.Failure("Path cannot be empty");
            }

            // Sanitize path
            var sanitizedPath = SanitizePath(path);
            if (sanitizedPath == null)
            {
                return Result.Failure("Invalid file path");
            }

            // Build full path
            var fullPath = Path.Combine(_basePath, sanitizedPath);

            // Validate path safety
            if (!IsPathSafe(fullPath))
            {
                return Result.Failure("Invalid file path - potential path traversal detected");
            }

            // Check if file exists
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", path);
                return Result.Failure("File not found");
            }

            // Delete file
            await Task.Run(() => File.Delete(fullPath), cancellationToken);

            _logger.LogInformation("Successfully deleted file: {FilePath}", path);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File delete operation was cancelled");
            return Result.Failure("Operation was cancelled");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while deleting file: {Path}", path);
            return Result.Failure($"Failed to delete file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while deleting file: {Path}", path);
            return Result.Failure("Unauthorized access to file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting file: {Path}", path);
            return Result.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    public async Task<Result<bool>> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Result<bool>.Failure("Path cannot be empty");
            }

            // Sanitize path
            var sanitizedPath = SanitizePath(path);
            if (sanitizedPath == null)
            {
                return Result<bool>.Failure("Invalid file path");
            }

            // Build full path
            var fullPath = Path.Combine(_basePath, sanitizedPath);

            // Validate path safety
            if (!IsPathSafe(fullPath))
            {
                return Result<bool>.Failure("Invalid file path - potential path traversal detected");
            }

            // Check if file exists
            var exists = await Task.Run(() => File.Exists(fullPath), cancellationToken);

            return Result<bool>.Success(exists);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File exists check was cancelled");
            return Result<bool>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking file existence: {Path}", path);
            return Result<bool>.Failure($"Error checking file existence: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitizes a file name by removing invalid characters
    /// </summary>
    private string? SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Get invalid characters for file names
        var invalidChars = Path.GetInvalidFileNameChars();

        // Remove invalid characters
        var sanitized = string.Join("", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Check for path traversal attempts
        if (sanitized.Contains("..") || sanitized.Contains("/") || sanitized.Contains("\\"))
        {
            return null;
        }

        // Ensure the filename is not empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return null;
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes a path by removing invalid characters and path traversal attempts
    /// </summary>
    private string? SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Normalize path separators
        var normalizedPath = path.Replace('\\', '/').Trim('/');

        // Split path into segments
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Check each segment for validity
        var sanitizedSegments = new List<string>();
        foreach (var segment in segments)
        {
            // Check for parent directory traversal
            if (segment == ".." || segment == ".")
            {
                return null;
            }

            // Get invalid characters for file/folder names
            var invalidChars = Path.GetInvalidFileNameChars();

            // Check if segment contains invalid characters
            if (segment.IndexOfAny(invalidChars) >= 0)
            {
                return null;
            }

            sanitizedSegments.Add(segment);
        }

        // Reconstruct path
        return string.Join(Path.DirectorySeparatorChar.ToString(), sanitizedSegments);
    }

    /// <summary>
    /// Validates that the resolved path is within the base path (prevents path traversal)
    /// </summary>
    private bool IsPathSafe(string fullPath)
    {
        try
        {
            // Get the fully resolved absolute path
            var resolvedPath = Path.GetFullPath(fullPath);
            var resolvedBasePath = Path.GetFullPath(_basePath);

            // Ensure the resolved path starts with the base path
            return resolvedPath.StartsWith(resolvedBasePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes an entire folder and its contents
    /// </summary>
    public async Task<Result> DeleteFolderAsync(
        string folder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return Result.Failure("Folder path cannot be empty");
            }

            // Sanitize folder path
            var sanitizedFolder = SanitizePath(folder);
            if (sanitizedFolder == null)
            {
                return Result.Failure("Invalid folder path");
            }

            // Build full path
            var fullPath = Path.Combine(_basePath, sanitizedFolder);

            // Validate path safety
            if (!IsPathSafe(fullPath))
            {
                return Result.Failure("Invalid folder path - potential path traversal detected");
            }

            // Check if directory exists
            if (!Directory.Exists(fullPath))
            {
                _logger.LogInformation("Folder does not exist, nothing to delete: {Folder}", folder);
                return Result.Success();
            }

            // Delete directory and all contents
            await Task.Run(() => Directory.Delete(fullPath, recursive: true), cancellationToken);

            _logger.LogInformation("Successfully deleted folder: {Folder}", folder);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Folder delete operation was cancelled");
            return Result.Failure("Operation was cancelled");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while deleting folder: {Folder}", folder);
            return Result.Failure($"Failed to delete folder: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while deleting folder: {Folder}", folder);
            return Result.Failure("Unauthorized access to folder");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting folder: {Folder}", folder);
            return Result.Failure($"Unexpected error: {ex.Message}");
        }
    }
}
