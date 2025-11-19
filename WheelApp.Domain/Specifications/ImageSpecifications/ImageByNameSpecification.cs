using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ImageSpecifications
{
    /// <summary>
    /// Specification to find images by name pattern
    /// </summary>
    public class ImageByNameSpecification : CompositeSpecification<Image>
    {
        private readonly string _namePattern;

        public ImageByNameSpecification(string namePattern)
        {
            _namePattern = namePattern;
        }

        public override Expression<Func<Image, bool>> ToExpression()
        {
            return image => image.Name.Contains(_namePattern);
        }
    }
}
