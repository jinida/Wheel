using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ProjectClassSpecifications
{
    /// <summary>
    /// Specification to find project classes by project ID
    /// </summary>
    public class ProjectClassByProjectIdSpecification : CompositeSpecification<ProjectClass>
    {
        private readonly int _projectId;

        public ProjectClassByProjectIdSpecification(int projectId)
        {
            _projectId = projectId;
        }

        public override Expression<Func<ProjectClass, bool>> ToExpression()
        {
            return projectClass => projectClass.ProjectId == _projectId;
        }
    }
}
