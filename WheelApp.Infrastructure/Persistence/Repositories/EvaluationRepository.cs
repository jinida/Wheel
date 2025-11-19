using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Evaluation repository implementation with specific query methods
    /// </summary>
    public class EvaluationRepository : RepositoryBase<Evaluation>, IEvaluationRepository
    {
        public EvaluationRepository(WheelAppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Evaluation>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet
                .AsNoTracking()
                .Where(e => e.TrainingId == trainingId)
                .ToListAsync(cancellationToken);
        }
    }
}
