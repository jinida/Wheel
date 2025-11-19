using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// ProjectClass repository implementation with specific query methods
    /// </summary>
    public class ProjectClassRepository : RepositoryBase<ProjectClass>, IProjectClassRepository
    {
        public ProjectClassRepository(WheelAppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<ProjectClass>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // Load classes and order in memory since ClassIdx is a value object
            // EF Core cannot translate OrderBy on value object properties
            var classes = await _dbSet
                .AsNoTracking()
                .Where(pc => pc.ProjectId == projectId)
                .ToListAsync(cancellationToken);

            return classes.OrderBy(pc => pc.ClassIdx.Value).ToList();
        }

        public async Task<ProjectClass?> GetByProjectIdAndClassIdxAsync(int projectId, int classIdx, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(pc => pc.ProjectId == projectId && pc.ClassIdx.Value == classIdx, cancellationToken);
        }

        public async Task<IReadOnlyList<ProjectClass>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return Array.Empty<ProjectClass>();

            // Fetch project classes by IDs in a single query (prevents N+1)
            return await _dbSet
                .AsNoTracking()
                .Where(pc => idList.Contains(pc.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProjectClass>> FindAsync(ISpecification<ProjectClass> specification, CancellationToken cancellationToken = default)
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
