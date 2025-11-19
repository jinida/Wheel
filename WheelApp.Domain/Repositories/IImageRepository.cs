using WheelApp.Domain.Entities;
using WheelApp.Domain.Specifications;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// Image-specific repository operations
    /// </summary>
    public interface IImageRepository : IRepository<Image>
    {
        Task<IReadOnlyList<Image>> GetByDatasetIdAsync(int datasetId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Image>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetCountsByDatasetAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetCountsByDatasetIdsAsync(List<int> datasetIds, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Image>> GetWithAnnotationsAsync(int datasetId, int projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Image>> FindAsync(ISpecification<Image> specification, CancellationToken cancellationToken = default);
    }
}
