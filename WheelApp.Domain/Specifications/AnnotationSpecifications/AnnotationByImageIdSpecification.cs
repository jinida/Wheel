using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.AnnotationSpecifications
{
    /// <summary>
    /// Specification to find annotations by image ID
    /// </summary>
    public class AnnotationByImageIdSpecification : CompositeSpecification<Annotation>
    {
        private readonly int _imageId;

        public AnnotationByImageIdSpecification(int imageId)
        {
            _imageId = imageId;
        }

        public override Expression<Func<Annotation, bool>> ToExpression()
        {
            return annotation => annotation.ImageId == _imageId;
        }
    }
}
