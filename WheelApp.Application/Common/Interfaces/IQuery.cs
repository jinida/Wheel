using MediatR;

namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries (CQRS read operations)
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
