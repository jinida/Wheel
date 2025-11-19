using WheelApp.Domain.Entities;
using WheelApp.Domain.Specifications;

namespace WheelApp.Domain.Repositories
{
    /// <summary>
    /// Annotation-specific repository operations
    /// </summary>
    public interface IAnnotationRepository : IRepository<Annotation>
    {
        Task<IReadOnlyList<Annotation>> GetByImageIdAsync(int imageId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Annotation>> GetByImageIdsAsync(IEnumerable<int> imageIds, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Annotation>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        Task<List<Annotation>> GetByProjectIdTrackingAsync(int projectId, CancellationToken cancellationToken = default);
        Task<Annotation?> GetByImageAndProjectAsync(int imageId, int projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Annotation>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<int> UpdateRangeAsync(IEnumerable<Annotation> annotations, CancellationToken cancellationToken = default);
        Task<int> DeleteRangeAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<int> DeleteByImageIdsAsync(IEnumerable<int> imageIds, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Annotation>> FindAsync(ISpecification<Annotation> specification, CancellationToken cancellationToken = default);
        Task<List<Annotation>> FindTrackingAsync(ISpecification<Annotation> specification, CancellationToken cancellationToken = default);
    }
}
