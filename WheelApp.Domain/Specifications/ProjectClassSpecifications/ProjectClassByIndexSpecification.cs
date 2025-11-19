using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.ProjectClassSpecifications
{
    /// <summary>
    /// Specification to find project class by class index
    /// </summary>
    public class ProjectClassByIndexSpecification : CompositeSpecification<ProjectClass>
    {
        private readonly int _classIndex;

        public ProjectClassByIndexSpecification(int classIndex)
        {
            _classIndex = classIndex;
        }

        public override Expression<Func<ProjectClass, bool>> ToExpression()
        {
            return projectClass => projectClass.ClassIdx.Value == _classIndex;
        }
    }
}
