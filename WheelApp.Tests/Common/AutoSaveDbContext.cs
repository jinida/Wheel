using Microsoft.EntityFrameworkCore;
using WheelApp.Infrastructure.Persistence;

namespace WheelApp.Tests.Common;

/// <summary>
/// DbContext wrapper that automatically saves changes after repository operations
/// Used to simulate TransactionBehavior in unit tests without modifying command handlers
/// </summary>
public class AutoSaveDbContext
{
    private readonly WheelAppDbContext _context;

    public AutoSaveDbContext(WheelAppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes an action and automatically saves changes if there are tracked modifications
    /// </summary>
    public async Task<T> ExecuteWithAutoSaveAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var result = await action();

        // Auto-save if there are changes
        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Executes an action and automatically saves changes if there are tracked modifications
    /// </summary>
    public async Task ExecuteWithAutoSaveAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await action();

        // Auto-save if there are changes
        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
