using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.AnnotationSpecifications
{
    /// <summary>
    /// Specification to find annotations for an image with class information eagerly loaded
    /// </summary>
    public class AnnotationsWithClassInfoSpecification : CompositeSpecification<Annotation>
    {
        private readonly int _imageId;

        public AnnotationsWithClassInfoSpecification(int imageId)
        {
            _imageId = imageId;

            // Eagerly load the class information
            AddInclude("Class");
        }

        public override Expression<Func<Annotation, bool>> ToExpression()
        {
            return annotation => annotation.ImageId == _imageId;
        }
    }
}
