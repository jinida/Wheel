using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Training repository implementation with specific query methods
    /// </summary>
    public class TrainingRepository : RepositoryBase<Training>, ITrainingRepository
    {
        public TrainingRepository(WheelAppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Override GetAllAsync to include Project and Dataset navigation properties
        /// </summary>
        public override async Task<IReadOnlyList<Training>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Dataset)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Training>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Training?> GetWithEvaluationsAsync(int id, CancellationToken cancellationToken = default)
        {
            // Include navigation property for eager loading
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Evaluations)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Training>> GetActiveTrainingsAsync(CancellationToken cancellationToken = default)
        {
            // Filter by Running status - parameterized
            var runningStatusValue = TrainingStatus.Running.Value;

            return await _dbSet
                .AsNoTracking()
                .Where(t => t.Status.Value == runningStatusValue)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Training>> FindAsync(ISpecification<Training> specification, CancellationToken cancellationToken = default)
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
