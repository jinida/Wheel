using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ProjectSpecifications
{
    /// <summary>
    /// Specification to retrieve all projects
    /// </summary>
    public class AllProjectsSpecification : CompositeSpecification<Project>
    {
        public override Expression<Func<Project, bool>> ToExpression()
        {
            return project => true;
        }
    }
}
