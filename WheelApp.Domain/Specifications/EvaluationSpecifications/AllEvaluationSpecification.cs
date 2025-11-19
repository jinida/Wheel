using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.EvaluationSpecifications
{
    /// <summary>
    /// Specification to find all evaluations
    /// </summary>
    public class AllEvaluationSpecification : CompositeSpecification<Evaluation>
    {
        public override Expression<Func<Evaluation, bool>> ToExpression()
        {
            return evaluation => true;
        }
    }
}
