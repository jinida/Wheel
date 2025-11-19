using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ImageSpecifications
{
    /// <summary>
    /// Specification to find images with their annotations eagerly loaded
    /// </summary>
    public class ImagesWithAnnotationsSpecification : CompositeSpecification<Image>
    {
        private readonly int _datasetId;

        public ImagesWithAnnotationsSpecification(int datasetId)
        {
            _datasetId = datasetId;

            // Add eager loading for annotations and their classes
            AddInclude(img => img.Annotations);
            AddInclude("Annotations.Class");
        }

        public override Expression<Func<Image, bool>> ToExpression()
        {
            return image => image.DatasetId == _datasetId;
        }
    }
}
