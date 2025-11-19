using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.RoleSpecifications
{
    /// <summary>
    /// Specification to find roles by multiple role IDs
    /// Used for batch operations to fetch multiple roles in a single query
    /// </summary>
    public class RolesByIdsSpecification : CompositeSpecification<Role>
    {
        private readonly List<int> _roleIds;

        public RolesByIdsSpecification(List<int> roleIds)
        {
            _roleIds = roleIds ?? new List<int>();
        }

        public override Expression<Func<Role, bool>> ToExpression()
        {
            return role => _roleIds.Contains(role.Id);
        }
    }
}
