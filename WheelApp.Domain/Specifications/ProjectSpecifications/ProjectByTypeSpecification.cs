using System.Linq.Expressions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Specifications.ProjectSpecifications
{
    /// <summary>
    /// Specification to find projects by type
    /// </summary>
    public class ProjectByTypeSpecification : CompositeSpecification<Project>
    {
        private readonly ProjectType _projectType;

        public ProjectByTypeSpecification(ProjectType projectType)
        {
            _projectType = projectType;
        }

        public override Expression<Func<Project, bool>> ToExpression()
        {
            return project => project.Type.Value == _projectType.Value;
        }
    }
}
