using MediatR;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;

namespace WheelApp.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for logging requests and responses
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId ?? "Anonymous";
        var userName = _currentUserService.UserName ?? "Anonymous";

        _logger.LogInformation("Handling {RequestName} by {UserName} ({UserId}) at {Timestamp}",
            requestName, userName, userId, DateTime.UtcNow);

        try
        {
            var response = await next();

            _logger.LogInformation("Handled {RequestName} successfully", requestName);

            return response;
        }
        catch (Exceptions.ValidationException vex)
        {
            var errorDetails = string.Join("; ", vex.Errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}"));
            _logger.LogError(vex, "Validation failed for {RequestName}: {ValidationErrors}",
                requestName, errorDetails);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestName}: {ErrorMessage}",
                requestName, ex.Message);
            throw;
        }
    }
}
