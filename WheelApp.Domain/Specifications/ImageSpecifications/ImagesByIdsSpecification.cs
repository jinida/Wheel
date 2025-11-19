using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ImageSpecifications
{
    /// <summary>
    /// Specification to find images by a list of IDs
    /// </summary>
    public class ImagesByIdsSpecification : CompositeSpecification<Image>
    {
        private readonly List<int> _imageIds;

        public ImagesByIdsSpecification(List<int> imageIds)
        {
            _imageIds = imageIds ?? new List<int>();
        }

        public override Expression<Func<Image, bool>> ToExpression()
        {
            return image => _imageIds.Contains(image.Id);
        }
    }
}
