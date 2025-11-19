using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.AnnotationSpecifications
{
    /// <summary>
    /// Specification to find annotations by class ID
    /// </summary>
    public class AnnotationByClassIdSpecification : CompositeSpecification<Annotation>
    {
        private readonly int _classId;

        public AnnotationByClassIdSpecification(int classId)
        {
            _classId = classId;
        }

        public override Expression<Func<Annotation, bool>> ToExpression()
        {
            return annotation => annotation.ClassId == _classId;
        }
    }
}
