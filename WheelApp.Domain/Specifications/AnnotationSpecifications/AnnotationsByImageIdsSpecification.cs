using System.Linq.Expressions;
using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Specifications.AnnotationSpecifications
{
    /// <summary>
    /// Specification to find annotations by multiple image IDs and project ID
    /// </summary>
    public class AnnotationsByImageIdsSpecification : CompositeSpecification<Annotation>
    {
        private readonly List<int> _imageIds;
        private readonly int _projectId;

        public AnnotationsByImageIdsSpecification(List<int> imageIds, int projectId)
        {
            _imageIds = imageIds ?? new List<int>();
            _projectId = projectId;
        }

        public override Expression<Func<Annotation, bool>> ToExpression()
        {
            return annotation => _imageIds.Contains(annotation.ImageId)
                              && annotation.ProjectId == _projectId;
        }
    }
}
