using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Project repository implementation with specific query methods
    /// </summary>
    public class ProjectRepository : RepositoryBase<Project>, IProjectRepository
    {
        public ProjectRepository(WheelAppDbContext context) : base(context)
        {
        }

        public override async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Include navigation properties for delete validation
            return await _dbSet
                .Include(p => p.Annotations)
                .Include(p => p.Classes)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Project>> GetByDatasetIdAsync(int datasetId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .Where(p => p.DatasetId == datasetId)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetByDatasetIdPagedAsync(
            int datasetId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Create base query
            var query = _dbSet
                .AsNoTracking()
                .Where(p => p.DatasetId == datasetId);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .OrderBy(p => p.Id)  // Ensure consistent ordering
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return new List<int>();

            // Return only IDs that exist in database - efficient query
            return await _dbSet
                .AsNoTracking()
                .Where(p => idList.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetCountsByDatasetAsync(CancellationToken cancellationToken = default)
        {
            // Group by dataset and count - all parameterized
            return await _dbSet
                .AsNoTracking()
                .GroupBy(p => p.DatasetId)
                .Select(g => new { DatasetId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DatasetId, x => x.Count, cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetCountsByDatasetIdsAsync(List<int> datasetIds, CancellationToken cancellationToken = default)
        {
            // OPTIMIZED: Only count for specific datasets (much faster than counting all)
            return await _dbSet
                .AsNoTracking()
                .Where(p => datasetIds.Contains(p.DatasetId))
                .GroupBy(p => p.DatasetId)
                .Select(g => new { DatasetId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DatasetId, x => x.Count, cancellationToken);
        }

        public async Task<Project?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            // Include navigation properties for eager loading
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Classes)
                .Include(p => p.Annotations)
                .Include(p => p.Trainings)
                .Include(p => p.Dataset)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Project>> GetByTypeAsync(ProjectType type, CancellationToken cancellationToken = default)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Filter by ProjectType value - parameterized
            return await _dbSet
                .AsNoTracking()
                .Where(p => p.Type.Value == type.Value)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Project>> FindAsync(ISpecification<Project> specification, CancellationToken cancellationToken = default)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            // Apply specification expression - automatically parameterized
            return await _dbSet
                .AsNoTracking()
                .Where(specification.ToExpression())
                .ToListAsync(cancellationToken);
        }
    }
}
