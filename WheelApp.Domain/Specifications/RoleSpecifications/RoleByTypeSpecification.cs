using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.RoleSpecifications
{
    /// <summary>
    /// Specification to find roles by role type (Train, Validation, Test)
    /// </summary>
    public class RoleByTypeSpecification : CompositeSpecification<Role>
    {
        private readonly RoleType _roleType;

        public RoleByTypeSpecification(RoleType roleType)
        {
            _roleType = roleType;
        }

        public override Expression<Func<Role, bool>> ToExpression()
        {
            return role => role.RoleType == _roleType;
        }
    }
}
