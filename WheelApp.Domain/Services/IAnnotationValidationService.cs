using WheelApp.Domain.Entities;
using WheelApp.Domain.Common;

namespace WheelApp.Domain.Services
{
    /// <summary>
    /// Domain service for annotation validation
    /// </summary>
    public interface IAnnotationValidationService
    {
        /// <summary>
        /// Validates annotation data format based on project type
        /// </summary>
        Task<Result> ValidateAnnotationFormat(Annotation annotation, Project project);

        /// <summary>
        /// Checks if annotation coordinates are within image boundaries
        /// </summary>
        Task<Result> ValidateAnnotationBounds(Annotation annotation, Image image);

        /// <summary>
        /// Validates if an annotation can be deleted
        /// </summary>
        Task<Result> CanDeleteAnnotation(Annotation annotation);

        /// <summary>
        /// Validates batch annotation operations
        /// </summary>
        Task<Result> ValidateBatchAnnotations(IEnumerable<Annotation> annotations, Project project);
    }
}
