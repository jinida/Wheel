using MediatR;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Services;

namespace WheelApp.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for transaction management
/// Only applies to commands (not queries) via ICommand marker interface
/// Uses DbContextConcurrencyGuard to prevent concurrent DbContext access in Blazor Server
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly DbContextConcurrencyGuard _concurrencyGuard;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        DbContextConcurrencyGuard concurrencyGuard,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _concurrencyGuard = concurrencyGuard;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Serialize database operations per user session to prevent concurrent DbContext access
        await _concurrencyGuard.Semaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Beginning transaction for {RequestName}", requestName);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var response = await next();
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                _logger.LogInformation("Transaction committed for {RequestName}", requestName);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed for {RequestName}, rolling back", requestName);
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            _concurrencyGuard.Semaphore.Release();
        }
    }
}
