using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.TrainingSpecifications
{
    /// <summary>
    /// Specification to find all trainings
    /// </summary>
    public class AllTrainingSpecification : CompositeSpecification<Training>
    {
        public override Expression<Func<Training, bool>> ToExpression()
        {
            return training => true;
        }
    }
}
