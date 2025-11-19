using WheelApp.Application.Common.Interfaces;

namespace WheelApp.Infrastructure.Services;

/// <summary>
/// Service for accessing current date and time (testable abstraction)
/// </summary>
public class DateTimeService : IDateTime
{
    /// <summary>
    /// Gets the current local date and time
    /// </summary>
    public DateTime Now => DateTime.Now;

    /// <summary>
    /// Gets the current UTC date and time
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}
