using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ImageSpecifications
{
    /// <summary>
    /// Specification to find images belonging to a specific dataset
    /// </summary>
    public class ImageByDatasetSpecification : CompositeSpecification<Image>
    {
        private readonly int _datasetId;

        public ImageByDatasetSpecification(int datasetId)
        {
            _datasetId = datasetId;
        }

        public override Expression<Func<Image, bool>> ToExpression()
        {
            return image => image.DatasetId == _datasetId;
        }
    }
}
