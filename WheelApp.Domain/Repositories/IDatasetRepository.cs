using WheelApp.Domain.Entities;
using WheelApp.Domain.Specifications;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// Dataset-specific repository operations
    /// </summary>
    public interface IDatasetRepository : IRepository<Dataset>
    {
        Task<Dataset?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Dataset>> GetWithImagesAsync(CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Dataset> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetImageCountsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Dataset>> FindAsync(ISpecification<Dataset> specification, CancellationToken cancellationToken = default);
    }
}
