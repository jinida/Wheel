using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.DatasetSpecifications
{
    /// <summary>
    /// Specification to find datasets that have images
    /// </summary>
    public class DatasetWithImagesSpecification : CompositeSpecification<Dataset>
    {
        public override Expression<Func<Dataset, bool>> ToExpression()
        {
            return dataset => dataset.Images.Any();
        }
    }
}
