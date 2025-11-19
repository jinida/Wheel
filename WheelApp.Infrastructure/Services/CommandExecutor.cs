using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WheelApp.Application.Common.Interfaces;

namespace WheelApp.Infrastructure.Services;

/// <summary>
/// Executes commands in isolated scopes to prevent DbContext concurrency issues
/// Each command execution gets its own scope with its own DbContext instance
/// </summary>
public class CommandExecutor : ICommandExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CommandExecutor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(IRequest<TResponse> command, CancellationToken cancellationToken = default)
    {
        // Create a new scope for this command execution
        using var scope = _scopeFactory.CreateScope();

        // Get a fresh IMediator instance from the new scope
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Execute the command with isolated services
        return await mediator.Send(command, cancellationToken);
    }
}