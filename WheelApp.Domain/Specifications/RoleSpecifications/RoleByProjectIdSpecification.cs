using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.RoleSpecifications
{
    /// <summary>
    /// Specification to find roles by project ID
    /// </summary>
    public class RoleByProjectIdSpecification : CompositeSpecification<Role>
    {
        private readonly int _projectId;

        public RoleByProjectIdSpecification(int projectId)
        {
            _projectId = projectId;
        }

        public override Expression<Func<Role, bool>> ToExpression()
        {
            return role => role.ProjectId == _projectId;
        }
    }
}
