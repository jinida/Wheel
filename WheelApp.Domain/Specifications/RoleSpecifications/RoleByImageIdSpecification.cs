using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.RoleSpecifications
{
    /// <summary>
    /// Specification to find roles by image ID
    /// </summary>
    public class RoleByImageIdSpecification : CompositeSpecification<Role>
    {
        private readonly int _imageId;

        public RoleByImageIdSpecification(int imageId)
        {
            _imageId = imageId;
        }

        public override Expression<Func<Role, bool>> ToExpression()
        {
            return role => role.ImageId == _imageId;
        }
    }
}
