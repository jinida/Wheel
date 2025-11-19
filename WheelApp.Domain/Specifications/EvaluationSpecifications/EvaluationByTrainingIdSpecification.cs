using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.EvaluationSpecifications
{
    /// <summary>
    /// Specification to find evaluations by training ID
    /// </summary>
    public class EvaluationByTrainingIdSpecification : CompositeSpecification<Evaluation>
    {
        private readonly int _trainingId;

        public EvaluationByTrainingIdSpecification(int trainingId)
        {
            _trainingId = trainingId;
        }

        public override Expression<Func<Evaluation, bool>> ToExpression()
        {
            return evaluation => evaluation.TrainingId == _trainingId;
        }
    }
}
