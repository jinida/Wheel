using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.TrainingSpecifications
{
    /// <summary>
    /// Specification to find completed trainings
    /// </summary>
    public class CompletedTrainingSpecification : CompositeSpecification<Training>
    {
        public override Expression<Func<Training, bool>> ToExpression()
        {
            return training => training.Status == TrainingStatus.Completed;
        }
    }
}
