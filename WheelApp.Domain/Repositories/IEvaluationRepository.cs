using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// Evaluation-specific repository operations
    /// </summary>
    public interface IEvaluationRepository : IRepository<Evaluation>
    {
        Task<IReadOnlyList<Evaluation>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);
    }
}
