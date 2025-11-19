using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.AnnotationSpecifications
{
    /// <summary>
    /// Specification to find annotations by both image ID and project ID
    /// Used to get all annotations for a specific image in a specific project
    /// </summary>
    public class AnnotationsByImageIdSpecification : CompositeSpecification<Annotation>
    {
        private readonly int _imageId;
        private readonly int _projectId;

        public AnnotationsByImageIdSpecification(int imageId, int projectId)
        {
            _imageId = imageId;
            _projectId = projectId;
        }

        public override Expression<Func<Annotation, bool>> ToExpression()
        {
            return annotation => annotation.ImageId == _imageId && annotation.ProjectId == _projectId;
        }
    }
}
