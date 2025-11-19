using WheelApp.Domain.Common;

namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// File storage abstraction for managing file operations
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves a file to storage
    /// </summary>
    /// <param name="stream">The file stream</param>
    /// <param name="folder">The destination folder</param>
    /// <param name="fileName">The file name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved file path</returns>
    Task<Result<string>> SaveAsync(Stream stream, string folder, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file from storage
    /// </summary>
    /// <param name="path">The file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The file stream</returns>
    Task<Result<Stream>> GetAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="path">The file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result> DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    /// <param name="path">The file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result<bool>> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entire folder from storage
    /// </summary>
    /// <param name="folder">The folder path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result> DeleteFolderAsync(string folder, CancellationToken cancellationToken = default);
}
