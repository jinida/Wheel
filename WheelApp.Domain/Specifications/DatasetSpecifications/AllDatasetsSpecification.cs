using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.DatasetSpecifications
{
    /// <summary>
    /// Specification to retrieve all datasets
    /// </summary>
    public class AllDatasetsSpecification : CompositeSpecification<Dataset>
    {
        public override Expression<Func<Dataset, bool>> ToExpression()
        {
            return dataset => true;
        }
    }
}
