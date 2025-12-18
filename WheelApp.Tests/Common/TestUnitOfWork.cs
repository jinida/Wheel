using WheelApp.Application.Common.Interfaces;
using WheelApp.Infrastructure.Persistence;

namespace WheelApp.Tests.Common;

/// <summary>
/// Test wrapper for UnitOfWork that automatically saves changes
/// This simulates TransactionBehavior in unit tests
/// </summary>
public class TestUnitOfWork : IUnitOfWork
{
    private readonly UnitOfWork _unitOfWork;
    private readonly WheelAppDbContext _context;

    public TestUnitOfWork(WheelAppDbContext context)
    {
        _context = context;
        _unitOfWork = new UnitOfWork(context);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        // Auto-save for InMemory DB tests
        await _context.SaveChangesAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _unitOfWork.RollbackAsync(cancellationToken);
    }

    public void ClearChangeTracker()
    {
        _unitOfWork.ClearChangeTracker();
    }

    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
    }
}
