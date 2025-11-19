namespace WheelApp.Application.Common.Interfaces;

/// <summary>
/// Unit of work pattern contract for managing transactions
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all changes to the database
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back current transaction
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the change tracker to stop tracking all entities
    /// </summary>
    void ClearChangeTracker();
}
