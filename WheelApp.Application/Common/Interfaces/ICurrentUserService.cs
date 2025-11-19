namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user's name
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Indicates whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
