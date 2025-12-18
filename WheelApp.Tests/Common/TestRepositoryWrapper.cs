using WheelApp.Domain.Common;
using WheelApp.Domain.Repositories;
using WheelApp.Infrastructure.Persistence;

namespace WheelApp.Tests.Common;

/// <summary>
/// Generic repository wrapper that auto-saves changes after mutations
/// Simulates TransactionBehavior for InMemory database tests
/// </summary>
public class TestRepositoryWrapper<T> : IRepository<T> where T : Entity
{
    private readonly IRepository<T> _innerRepository;
    private readonly WheelAppDbContext _context;

    public TestRepositoryWrapper(IRepository<T> innerRepository, WheelAppDbContext context)
    {
        _innerRepository = innerRepository;
        _context = context;
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _innerRepository.GetAllAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.ExistsAsync(id, cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _innerRepository.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _innerRepository.UpdateAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteByIdAsync(id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void Detach(T entity)
    {
        _innerRepository.Detach(entity);
    }
}
