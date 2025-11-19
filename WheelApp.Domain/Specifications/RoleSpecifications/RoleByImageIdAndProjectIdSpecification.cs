using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.RoleSpecifications
{
    /// <summary>
    /// Specification to find roles by both image ID and project ID
    /// Used to get the role assignment for a specific image in a specific project
    /// </summary>
    public class RoleByImageIdAndProjectIdSpecification : CompositeSpecification<Role>
    {
        private readonly int _imageId;
        private readonly int _projectId;

        public RoleByImageIdAndProjectIdSpecification(int imageId, int projectId)
        {
            _imageId = imageId;
            _projectId = projectId;
        }

        public override Expression<Func<Role, bool>> ToExpression()
        {
            return role => role.ImageId == _imageId && role.ProjectId == _projectId;
        }
    }
}
