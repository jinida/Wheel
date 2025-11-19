using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.TrainingSpecifications
{
    /// <summary>
    /// Specification to find trainings by status
    /// </summary>
    public class TrainingByStatusSpecification : CompositeSpecification<Training>
    {
        private readonly TrainingStatus _status;

        public TrainingByStatusSpecification(TrainingStatus status)
        {
            _status = status;
        }

        public override Expression<Func<Training, bool>> ToExpression()
        {
            return training => training.Status == _status;
        }
    }
}
