using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Role repository implementation with specific query methods
    /// </summary>
    public class RoleRepository : RepositoryBase<Role>, IRoleRepository
    {
        public RoleRepository(WheelAppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Role>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .Where(r => r.ProjectId == projectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Role>> GetByProjectIdTrackingAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // Returns tracked entities for update operations
            return await _dbSet
                .Where(r => r.ProjectId == projectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return Array.Empty<Role>();

            // Fetch roles by IDs in a single query (prevents N+1)
            return await _dbSet
                .Where(r => idList.Contains(r.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Role>> GetByImageIdsAsync(IEnumerable<int> imageIds, CancellationToken cancellationToken = default)
        {
            if (imageIds == null)
                throw new ArgumentNullException(nameof(imageIds));

            var imageIdList = imageIds.ToList();
            if (!imageIdList.Any())
                return Array.Empty<Role>();

            // Fetch roles by image IDs in a single query (prevents N+1)
            return await _dbSet
                .AsNoTracking()
                .Where(r => imageIdList.Contains(r.ImageId))
                .ToListAsync(cancellationToken);
        }

        public async Task<Role?> GetByImageAndProjectAsync(int imageId, int projectId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ImageId == imageId && r.ProjectId == projectId, cancellationToken);
        }

        public async Task<IReadOnlyList<Role>> GetByRoleTypeAsync(int projectId, RoleType roleType, CancellationToken cancellationToken = default)
        {
            if (roleType == null)
                throw new ArgumentNullException(nameof(roleType));

            // Filter by project and role type - parameterized
            return await _dbSet
                .AsNoTracking()
                .Where(r => r.ProjectId == projectId && r.RoleType.Value == roleType.Value)
                .ToListAsync(cancellationToken);
        }

        public Task<int> BatchUpdateAsync(IEnumerable<Role> roles, CancellationToken cancellationToken = default)
        {
            if (roles == null)
                throw new ArgumentNullException(nameof(roles));

            var roleList = roles.ToList();
            if (!roleList.Any())
                return Task.FromResult(0);

            _dbSet.UpdateRange(roleList);
            return Task.FromResult(roleList.Count);
        }

        public async Task<int> BatchDeleteAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return 0;

            // Fetch entities to delete - parameterized query
            var entitiesToDelete = await _dbSet
                .Where(r => idList.Contains(r.Id))
                .ToListAsync(cancellationToken);

            if (entitiesToDelete.Any())
            {
                _dbSet.RemoveRange(entitiesToDelete);
            }

            return entitiesToDelete.Count;
        }

        public async Task<IReadOnlyList<Role>> FindAsync(ISpecification<Role> specification, CancellationToken cancellationToken = default)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            // Apply specification expression - automatically parameterized
            return await _dbSet
                .AsNoTracking()
                .Where(specification.ToExpression())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Role>> FindTrackingAsync(ISpecification<Role> specification, CancellationToken cancellationToken = default)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            // Apply specification expression WITH TRACKING for update operations
            return await _dbSet
                .Where(specification.ToExpression())
                .ToListAsync(cancellationToken);
        }
    }
}
