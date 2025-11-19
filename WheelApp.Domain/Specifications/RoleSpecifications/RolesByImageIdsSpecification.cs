using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.RoleSpecifications
{
    /// <summary>
    /// Specification to find roles by multiple image IDs and project ID
    /// </summary>
    public class RolesByImageIdsSpecification : CompositeSpecification<Role>
    {
        private readonly List<int> _imageIds;
        private readonly int _projectId;

        public RolesByImageIdsSpecification(List<int> imageIds, int projectId)
        {
            _imageIds = imageIds ?? new List<int>();
            _projectId = projectId;
        }

        public override Expression<Func<Role, bool>> ToExpression()
        {
            return role => _imageIds.Contains(role.ImageId)
                        && role.ProjectId == _projectId;
        }
    }
}
