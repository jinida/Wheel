using WheelApp.Domain.Entities;
using WheelApp.Domain.Specifications;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// Training-specific repository operations
    /// </summary>
    public interface ITrainingRepository : IRepository<Training>
    {
        Task<IReadOnlyList<Training>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        Task<Training?> GetWithEvaluationsAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Training>> GetActiveTrainingsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Training>> FindAsync(ISpecification<Training> specification, CancellationToken cancellationToken = default);
    }
}
