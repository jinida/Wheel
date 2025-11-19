using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.TrainingSpecifications
{
    /// <summary>
    /// Specification to find trainings by project ID
    /// </summary>
    public class TrainingByProjectIdSpecification : CompositeSpecification<Training>
    {
        private readonly int _projectId;

        public TrainingByProjectIdSpecification(int projectId)
        {
            _projectId = projectId;
        }

        public override Expression<Func<Training, bool>> ToExpression()
        {
            return training => training.ProjectId == _projectId;
        }
    }
}
