using MediatR;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Executes commands in an isolated scope to prevent DbContext concurrency issues in Blazor Server
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes a command in a new isolated scope
    /// </summary>
    Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> command, CancellationToken cancellationToken = default);
}