using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.DatasetSpecifications
{
    /// <summary>
    /// Specification to find datasets created after a specific date
    /// </summary>
    public class DatasetCreatedAfterSpecification : CompositeSpecification<Dataset>
    {
        private readonly DateTime _date;

        public DatasetCreatedAfterSpecification(DateTime date)
        {
            _date = date;
        }

        public override Expression<Func<Dataset, bool>> ToExpression()
        {
            return dataset => dataset.CreatedAt > _date;
        }
    }
}
