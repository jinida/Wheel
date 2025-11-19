using WheelApp.Domain.Entities;
using WheelApp.Domain.Specifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// Project-specific repository operations
    /// </summary>
    public interface IProjectRepository : IRepository<Project>
    {
        Task<IReadOnlyList<Project>> GetByDatasetIdAsync(int datasetId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Project> Items, int TotalCount)> GetByDatasetIdPagedAsync(int datasetId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetCountsByDatasetAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetCountsByDatasetIdsAsync(List<int> datasetIds, CancellationToken cancellationToken = default);
        Task<Project?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Project>> GetByTypeAsync(ProjectType type, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Project>> FindAsync(ISpecification<Project> specification, CancellationToken cancellationToken = default);
    }
}
