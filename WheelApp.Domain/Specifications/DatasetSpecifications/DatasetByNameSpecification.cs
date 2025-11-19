using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.DatasetSpecifications
{
    /// <summary>
    /// Specification to find datasets by name pattern
    /// </summary>
    public class DatasetByNameSpecification : CompositeSpecification<Dataset>
    {
        private readonly string _namePattern;

        public DatasetByNameSpecification(string namePattern)
        {
            _namePattern = namePattern;
        }

        public override Expression<Func<Dataset, bool>> ToExpression()
        {
            return dataset => dataset.Name.Value.Contains(_namePattern);
        }
    }
}
