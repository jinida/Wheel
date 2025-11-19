using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.AnnotationSpecifications
{
    /// <summary>
    /// Specification to find annotations by project ID
    /// </summary>
    public class AnnotationByProjectIdSpecification : CompositeSpecification<Annotation>
    {
        private readonly int _projectId;

        public AnnotationByProjectIdSpecification(int projectId)
        {
            _projectId = projectId;
        }

        public override Expression<Func<Annotation, bool>> ToExpression()
        {
            return annotation => annotation.ProjectId == _projectId;
        }
    }
}
