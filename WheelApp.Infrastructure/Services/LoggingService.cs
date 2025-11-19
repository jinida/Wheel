using Microsoft.Extensions.Logging;

namespace WheelApp.Infrastructure.Services;

/// <summary>
/// Service for structured logging (wrapper around ILogger)
/// </summary>
public class LoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs an informational message
    /// </summary>
    /// <param name="message">The message to log</param>
    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="message">The message to log</param>
    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }

    /// <summary>
    /// Logs an error message
    /// </summary>
    /// <param name="message">The message to log</param>
    public void LogError(string message)
    {
        _logger.LogError(message);
    }
}
