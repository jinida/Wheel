using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Dataset repository implementation with specific query methods
    /// </summary>
    public class DatasetRepository : RepositoryBase<Dataset>, IDatasetRepository
    {
        public DatasetRepository(WheelAppDbContext context) : base(context)
        {
        }

        public override async Task<Dataset?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Include navigation properties for delete validation
            return await _dbSet
                .AsNoTracking()
                .Include(d => d.Images)
                .Include(d => d.Projects)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<Dataset?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Solution: Load all datasets and filter client-side
            // This is necessary because EF Core cannot translate operations on value object properties
            // For better performance with large datasets, consider using raw SQL or stored procedure
            var datasets = await _dbSet
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return datasets.FirstOrDefault(d =>
                string.Equals(d.Name.Value, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IReadOnlyList<Dataset>> GetWithImagesAsync(CancellationToken cancellationToken = default)
        {
            // Include navigation property for eager loading
            return await _dbSet
                .AsNoTracking()
                .Include(d => d.Images)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Dataset> Items, int TotalCount)> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Create base query
            var query = _dbSet.AsNoTracking();

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .OrderBy(d => d.Id)  // Ensure consistent ordering
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Dictionary<int, int>> GetImageCountsAsync(CancellationToken cancellationToken = default)
        {
            // Group by dataset ID and count images - all parameterized
            return await _context.Images
                .AsNoTracking()
                .GroupBy(i => i.DatasetId)
                .Select(g => new { DatasetId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DatasetId, x => x.Count, cancellationToken);
        }

        public async Task<IReadOnlyList<Dataset>> FindAsync(ISpecification<Dataset> specification, CancellationToken cancellationToken = default)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            // Apply specification expression - automatically parameterized
            // Include Images for specifications that need them (e.g., DatasetWithImagesSpecification)
            return await _dbSet
                .AsNoTracking()
                .Include(d => d.Images)
                .Where(specification.ToExpression())
                .ToListAsync(cancellationToken);
        }
    }
}
