namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Testable date/time service abstraction
/// </summary>
public interface IDateTime
{
    /// <summary>
    /// Gets the current local date and time
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current UTC date and time
    /// </summary>
    DateTime UtcNow { get; }
}
