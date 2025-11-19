using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Image repository implementation with specific query methods
    /// </summary>
    public class ImageRepository : RepositoryBase<Image>, IImageRepository
    {
        public ImageRepository(WheelAppDbContext context) : base(context)
        {
        }

        public override async Task<Image?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Include Annotations collection for HasAnnotations flag to work correctly
            return await _dbSet
                .Include(i => i.Annotations)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Image>> GetByDatasetIdAsync(int datasetId, CancellationToken cancellationToken = default)
        {
            // Include Annotations collection for HasAnnotations flag to work correctly
            return await _dbSet
                .AsNoTracking()
                .Include(i => i.Annotations)
                .Where(i => i.DatasetId == datasetId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Image>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (!idList.Any())
                return Array.Empty<Image>();

            // Fetch images by IDs in a single query (prevents N+1)
            return await _dbSet
                .AsNoTracking()
                .Where(i => idList.Contains(i.Id))
                .ToListAsync(cancellationToken);
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
                .Where(i => idList.Contains(i.Id))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetCountsByDatasetAsync(CancellationToken cancellationToken = default)
        {
            // Group by dataset and count - all parameterized
            return await _dbSet
                .AsNoTracking()
                .GroupBy(i => i.DatasetId)
                .Select(g => new { DatasetId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DatasetId, x => x.Count, cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetCountsByDatasetIdsAsync(List<int> datasetIds, CancellationToken cancellationToken = default)
        {
            // OPTIMIZED: Only count for specific datasets (much faster than counting all)
            return await _dbSet
                .AsNoTracking()
                .Where(i => datasetIds.Contains(i.DatasetId))
                .GroupBy(i => i.DatasetId)
                .Select(g => new { DatasetId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DatasetId, x => x.Count, cancellationToken);
        }

        /// <summary>
        /// Gets images with their annotations filtered by project.
        /// </summary>
        /// <param name="datasetId">The dataset ID to filter images by</param>
        /// <param name="projectId">The project ID to filter annotations by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Images with only annotations belonging to the specified project</returns>
        /// <remarks>
        /// IMPORTANT: This method uses filtered Include, which means the Annotations collection
        /// on the returned Image entities will ONLY contain annotations for the specified projectId.
        /// It does NOT contain all annotations for each image.
        ///
        /// Use this method when you specifically need images with project-filtered annotations.
        /// If you need all annotations for an image, use GetByIdAsync and include all annotations separately.
        /// </remarks>
        public async Task<IReadOnlyList<Image>> GetWithAnnotationsAsync(int datasetId, int projectId, CancellationToken cancellationToken = default)
        {
            // Include annotations with filters - all parameterized
            // WARNING: This filters the Annotations collection on the entity
            return await _dbSet
                .AsNoTracking()
                .Where(i => i.DatasetId == datasetId)
                .Include(i => i.Annotations.Where(a => a.ProjectId == projectId))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Image>> FindAsync(ISpecification<Image> specification, CancellationToken cancellationToken = default)
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
