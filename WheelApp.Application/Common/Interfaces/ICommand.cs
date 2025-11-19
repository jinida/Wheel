using MediatR;

namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands (CQRS write operations)
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
