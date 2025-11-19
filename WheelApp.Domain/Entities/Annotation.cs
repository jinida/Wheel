using WheelApp.Domain.Common;
using WheelApp.Domain.Events.AnnotationEvents;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// Annotation entity for image labeling
    /// Stores annotation data as JSON string
    /// NULL for classification and anomaly detection, coordinate array for detection/segmentation
    /// </summary>
    public class Annotation : Entity
    {
        public int ImageId { get; private set; }
        public int ProjectId { get; private set; }
        public int ClassId { get; private set; }
        public string? Information { get; private set; }  // JSON array [x1, y1, ..., xn, yn] or NULL
        public DateTime CreatedAt { get; private set; }

        // Navigation properties
        public Image? Image { get; private set; }  // EF Core navigation property
        public Project? Project { get; private set; }  // EF Core navigation property
        public ProjectClass? ProjectClass { get; private set; }  // EF Core navigation property

        private Annotation() { }  // For EF Core

        private Annotation(int imageId, int projectId, int classId, string? information)
        {
            ImageId = imageId;
            ProjectId = projectId;
            ClassId = classId;
            Information = information;
            CreatedAt = DateTime.UtcNow;
            AddDomainEvent(new AnnotationCreatedEvent(this));
        }

        /// <summary>
        /// Factory method to create a new annotation
        /// </summary>
        public static Annotation Create(int imageId, int projectId, int classId, string? information = null)
        {
            if (imageId <= 0)
                throw new ValidationException(nameof(imageId), "Image ID must be positive.");

            if (projectId <= 0)
                throw new ValidationException(nameof(projectId), "Project ID must be positive.");

            if (classId <= 0)
                throw new ValidationException(nameof(classId), "Class ID must be positive.");

            return new Annotation(imageId, projectId, classId, information);
        }

        /// <summary>
        /// Updates the annotation information
        /// </summary>
        public void UpdateAnnotation(string? information)
        {
            var oldInformation = Information;
            Information = information;
            AddDomainEvent(new AnnotationUpdatedEvent(this, oldInformation ?? "", information ?? ""));
        }

        /// <summary>
        /// Changes the class assignment
        /// </summary>
        public void ChangeClass(int newClassId)
        {
            if (newClassId <= 0)
                throw new ValidationException(nameof(newClassId), "Class ID must be positive.");

            ClassId = newClassId;
        }
    }
}
