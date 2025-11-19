using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// Image entity representing an image in the dataset
    /// </summary>
    public class Image : Entity
    {
        private readonly List<Annotation> _annotations = new();

        public string Name { get; private set; }
        public FilePath Path { get; private set; }
        public int DatasetId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public IReadOnlyCollection<Annotation> Annotations => _annotations.AsReadOnly();

        private Image() { }  // For EF Core

        private Image(string name, FilePath path, int datasetId)
        {
            Name = name;
            Path = path;
            DatasetId = datasetId;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Factory method to create a new image
        /// </summary>
        public static Image Create(string name, string path, int datasetId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException(nameof(name), "Image name cannot be empty.");

            if (name.Length > 50)
                throw new ValidationException(nameof(name), "Image name cannot exceed 50 characters.");

            var filePath = FilePath.Create(path);
            return new Image(name, filePath, datasetId);
        }

        /// <summary>
        /// Adds an annotation to the image
        /// </summary>
        public void AddAnnotation(Annotation annotation)
        {
            if (annotation == null)
                throw new ValidationException(nameof(annotation), "Annotation cannot be null.");

            _annotations.Add(annotation);
        }

        /// <summary>
        /// Updates the image name
        /// </summary>
        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ValidationException(nameof(newName), "Image name cannot be empty.");

            if (newName.Length > 50)
                throw new ValidationException(nameof(newName), "Image name cannot exceed 50 characters.");

            Name = newName;
        }
    }
}
