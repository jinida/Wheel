using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.TrainingSpecifications
{
    /// <summary>
    /// Specification to find active (running) trainings
    /// </summary>
    public class ActiveTrainingSpecification : CompositeSpecification<Training>
    {
        public override Expression<Func<Training, bool>> ToExpression()
        {
            return training => training.Status == TrainingStatus.Running;
        }
    }
}
