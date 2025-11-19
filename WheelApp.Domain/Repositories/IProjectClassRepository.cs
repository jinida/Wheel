using WheelApp.Domain.Entities;
using WheelApp.Domain.Specifications;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// ProjectClass-specific repository operations
    /// </summary>
    public interface IProjectClassRepository : IRepository<ProjectClass>
    {
        Task<IReadOnlyList<ProjectClass>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        Task<ProjectClass?> GetByProjectIdAndClassIdxAsync(int projectId, int classIdx, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProjectClass>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProjectClass>> FindAsync(ISpecification<ProjectClass> specification, CancellationToken cancellationToken = default);
    }
}