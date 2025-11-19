using WheelApp.Domain.Common;
using WheelApp.Domain.Events.DatasetEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// Dataset aggregate root for managing collections of images
    /// </summary>
    public class Dataset : AuditableEntity
    {
        private readonly List<Image> _images = new();
        private readonly List<Project> _projects = new();

        public DatasetName Name { get; private set; }
        public Description Description { get; private set; }

        public IReadOnlyCollection<Image> Images => _images.AsReadOnly();
        public IReadOnlyCollection<Project> Projects => _projects.AsReadOnly();
        public int ImageCount => _images.Count;

        private Dataset() { }  // For EF Core

        private Dataset(DatasetName name, Description description, string createdBy)
        {
            Name = name;
            Description = description;
            // Audit fields (CreatedAt, CreatedBy) are automatically set by AuditInterceptor
            AddDomainEvent(new DatasetCreatedEvent(this));
        }

        /// <summary>
        /// Factory method to create a new dataset
        /// </summary>
        public static Dataset Create(string name, string? description, string createdBy)
        {
            var datasetName = DatasetName.Create(name);
            var desc = Description.Create(description);
            return new Dataset(datasetName, desc, createdBy);
        }

        /// <summary>
        /// Adds an image to the dataset
        /// </summary>
        public void AddImage(Image image)
        {
            if (image == null)
                throw new ValidationException(nameof(image), "Image cannot be null.");

            // Note: Duplicate check is enforced by database unique constraint on Path
            // This prevents race conditions and ensures consistency
            _images.Add(image);
            AddDomainEvent(new ImageAddedToDatasetEvent(this, image));
        }

        /// <summary>
        /// Removes an image from the dataset
        /// </summary>
        public void RemoveImage(int imageId)
        {
            var image = _images.FirstOrDefault(i => i.Id == imageId);
            if (image == null)
                throw new EntityNotFoundException(nameof(Image), imageId);

            _images.Remove(image);
            AddDomainEvent(new ImageRemovedFromDatasetEvent(this, imageId));
        }

        /// <summary>
        /// Updates the dataset name
        /// </summary>
        public void UpdateName(string newName, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(modifiedBy))
                throw new ValidationException(nameof(modifiedBy), "Modified by cannot be empty.");

            var oldName = Name.Value;
            Name = DatasetName.Create(newName);
            UpdateAudit(modifiedBy);

            var changes = new Dictionary<string, object>
            {
                { nameof(Name), $"{oldName} -> {newName}" }
            };
            AddDomainEvent(new DatasetUpdatedEvent(this, changes));
        }

        /// <summary>
        /// Updates the description
        /// </summary>
        public void UpdateDescription(string? newDescription, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(modifiedBy))
                throw new ValidationException(nameof(modifiedBy), "Modified by cannot be empty.");

            Description = Description.Create(newDescription);
            UpdateAudit(modifiedBy);

            var changes = new Dictionary<string, object>
            {
                { nameof(Description), newDescription ?? "" }
            };
            AddDomainEvent(new DatasetUpdatedEvent(this, changes));
        }

        /// <summary>
        /// Associates a project with this dataset
        /// </summary>
        public void AddProject(Project project)
        {
            if (project == null)
                throw new ValidationException(nameof(project), "Project cannot be null.");

            if (_projects.Any(p => p.Id == project.Id && project.Id != 0))
                throw new DuplicateEntityException(nameof(Project), $"Project with ID {project.Id}");

            _projects.Add(project);

            // Maintain bidirectional relationship
            if (project.Dataset != this)
            {
                project.SetDataset(this);
            }
        }
    }
}
