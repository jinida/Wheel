using MediatR;

namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Mediator wrapper that creates a new DI scope for each command execution
/// This ensures complete isolation in Blazor Server scenarios
/// </summary>
public interface IScopedMediator
{
    /// <summary>
    /// Sends a request within a new DI scope
    /// </summary>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification within a new DI scope
    /// </summary>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}