using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ProjectSpecifications
{
    /// <summary>
    /// Specification to find projects by name pattern
    /// </summary>
    public class ProjectByNameSpecification : CompositeSpecification<Project>
    {
        private readonly string _namePattern;

        public ProjectByNameSpecification(string namePattern)
        {
            _namePattern = namePattern;
        }

        public override Expression<Func<Project, bool>> ToExpression()
        {
            return project => project.Name.Value.Contains(_namePattern);
        }
    }
}
