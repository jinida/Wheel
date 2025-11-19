using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ProjectClassSpecifications
{
    /// <summary>
    /// Specification to find project classes by name
    /// </summary>
    public class ProjectClassByNameSpecification : CompositeSpecification<ProjectClass>
    {
        private readonly string _name;

        public ProjectClassByNameSpecification(string name)
        {
            _name = name;
        }

        public override Expression<Func<ProjectClass, bool>> ToExpression()
        {
            return projectClass => projectClass.Name == _name;
        }
    }
}
