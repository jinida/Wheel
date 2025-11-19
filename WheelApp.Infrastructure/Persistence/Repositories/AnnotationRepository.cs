using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Annotation repository implementation with specific query methods
    /// </summary>
    public class AnnotationRepository : RepositoryBase<Annotation>, IAnnotationRepository
    {
        public AnnotationRepository(WheelAppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Annotation>> GetByImageIdAsync(int imageId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .Where(a => a.ImageId == imageId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Annotation>> GetByImageIdsAsync(IEnumerable<int> imageIds, CancellationToken cancellationToken = default)
        {
            if (imageIds == null)
                throw new ArgumentNullException(nameof(imageIds));

            var imageIdList = imageIds.ToList();
            if (!imageIdList.Any())
                return Array.Empty<Annotation>();

            // Fetch annotations by image IDs in a single query (prevents N+1)
            return await _dbSet
                .AsNoTracking()
                .Where(a => imageIdList.Contains(a.ImageId))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Annotation>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .Where(a => a.ProjectId == projectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Annotation>> GetByProjectIdTrackedAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // Return tracked entities so they can be deleted
            return await _dbSet
                .Where(a => a.ProjectId == projectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Annotation>> GetByProjectIdTrackingAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // Returns tracked entities for update operations
            return await _dbSet
                .Where(a => a.ProjectId == projectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Annotation?> GetByImageAndProjectAsync(int imageId, int projectId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ImageId == imageId && a.ProjectId == projectId, cancellationToken);
        }

        public async Task<IReadOnlyList<Annotation>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return new List<Annotation>();

            // EF Core uses parameterized queries by default
            return await _dbSet
                .Where(a => idList.Contains(a.Id))
                .ToListAsync(cancellationToken);
        }

        public Task<int> UpdateRangeAsync(IEnumerable<Annotation> annotations, CancellationToken cancellationToken = default)
        {
            if (annotations == null)
                throw new ArgumentNullException(nameof(annotations));

            var annotationList = annotations.ToList();
            if (!annotationList.Any())
                return Task.FromResult(0);

            _dbSet.UpdateRange(annotationList);
            return Task.FromResult(annotationList.Count);
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return 0;

            // Fetch entities to delete - parameterized query
            var entitiesToDelete = await _dbSet
                .Where(a => idList.Contains(a.Id))
                .ToListAsync(cancellationToken);

            if (entitiesToDelete.Any())
            {
                _dbSet.RemoveRange(entitiesToDelete);
            }

            return entitiesToDelete.Count;
        }

        public async Task<int> DeleteByImageIdsAsync(IEnumerable<int> imageIds, CancellationToken cancellationToken = default)
        {
            var imageIdList = imageIds.ToList();
            if (!imageIdList.Any())
                return 0;

            // Fetch all annotations for the specified image IDs - parameterized query
            var annotationsToDelete = await _dbSet
                .Where(a => imageIdList.Contains(a.ImageId))
                .ToListAsync(cancellationToken);

            if (annotationsToDelete.Any())
            {
                _dbSet.RemoveRange(annotationsToDelete);
            }

            return annotationsToDelete.Count;
        }

        public async Task<IReadOnlyList<Annotation>> FindAsync(ISpecification<Annotation> specification, CancellationToken cancellationToken = default)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            // Apply specification expression - automatically parameterized
            return await _dbSet
                .AsNoTracking()
                .Where(specification.ToExpression())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Annotation>> FindTrackingAsync(ISpecification<Annotation> specification, CancellationToken cancellationToken = default)
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
