using WheelApp.Domain.Entities;
using WheelApp.Domain.Specifications;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// Role-specific repository operations
    /// </summary>
    public interface IRoleRepository : IRepository<Role>
    {
        Task<IReadOnlyList<Role>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        Task<List<Role>> GetByProjectIdTrackingAsync(int projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Role>> GetByImageIdsAsync(IEnumerable<int> imageIds, CancellationToken cancellationToken = default);
        Task<Role?> GetByImageAndProjectAsync(int imageId, int projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Role>> GetByRoleTypeAsync(int projectId, RoleType roleType, CancellationToken cancellationToken = default);
        Task<int> BatchUpdateAsync(IEnumerable<Role> roles, CancellationToken cancellationToken = default);
        Task<int> BatchDeleteAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Role>> FindAsync(ISpecification<Role> specification, CancellationToken cancellationToken = default);
        Task<List<Role>> FindTrackingAsync(ISpecification<Role> specification, CancellationToken cancellationToken = default);
    }
}
