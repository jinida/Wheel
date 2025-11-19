using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.RoleSpecifications
{
    /// <summary>
    /// Specification to find roles assigned to validation set
    /// </summary>
    public class ValidationRoleSpecification : CompositeSpecification<Role>
    {
        public override Expression<Func<Role, bool>> ToExpression()
        {
            return role => role.RoleType == RoleType.Validation;
        }
    }
}
