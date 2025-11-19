using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WheelApp.Application.Common.Interfaces;

namespace WheelApp.Infrastructure.Persistence
{
    /// <summary>
    /// Unit of Work implementation wrapping a Scoped DbContext.
    /// Ensures all repositories share the same DbContext instance for consistent transactions.
    /// </summary>
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly WheelAppDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(WheelAppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private WheelAppDbContext Context => _context;

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            // Check if there's already an active transaction
            if (_transaction != null)
            {
                return; // Already in a transaction
            }

            // Check if DbContext already has a transaction
            if (Context.Database.CurrentTransaction != null)
            {
                _transaction = Context.Database.CurrentTransaction;
                return;
            }

            // Start new transaction
            _transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await CommitAsync(cancellationToken);
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                try
                {
                    // Only rollback if transaction is still active
                    if (Context.Database.CurrentTransaction != null)
                    {
                        await _transaction.RollbackAsync(cancellationToken);
                    }
                }
                catch
                {
                    // Ignore rollback errors - transaction might already be dead
                }
                finally
                {
                    try
                    {
                        await _transaction.DisposeAsync();
                    }
                    catch
                    {
                        // Ignore dispose errors
                    }
                    _transaction = null;
                }
            }

            // Clear change tracker to discard all tracked changes
            _context.ChangeTracker.Clear();
        }

        public void ClearChangeTracker()
        {
            Context.ChangeTracker.Clear();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            // DbContext is managed by DI container (Scoped lifetime)
            // No need to manually dispose it here
        }
    }
}
