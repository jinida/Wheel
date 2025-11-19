using WheelApp.Domain.Common;
using WheelApp.Domain.Events.ProjectEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// Project aggregate root for AI project management
    /// </summary>
    public class Project : AuditableEntity
    {
        private readonly List<ProjectClass> _classes = new();
        private readonly List<Annotation> _annotations = new();
        private readonly List<Training> _trainings = new();

        public ProjectName Name { get; private set; }
        public ProjectType Type { get; private set; }
        public Description Description { get; private set; }
        public int DatasetId { get; private set; }

        // Navigation properties
        public Dataset? Dataset { get; private set; }  // EF Core navigation property

        public IReadOnlyCollection<ProjectClass> Classes => _classes.AsReadOnly();
        public IReadOnlyCollection<Annotation> Annotations => _annotations.AsReadOnly();
        public IReadOnlyCollection<Training> Trainings => _trainings.AsReadOnly();

        private Project() { }  // For EF Core

        private Project(ProjectName name, ProjectType type, Description description, int datasetId, string createdBy)
        {
            Name = name;
            Type = type;
            Description = description;
            DatasetId = datasetId;
            // Audit fields (CreatedAt, CreatedBy) are automatically set by AuditInterceptor
            AddDomainEvent(new ProjectCreatedEvent(this));
        }

        /// <summary>
        /// Factory method to create a new project
        /// </summary>
        public static Project Create(string name, int projectType, string? description, int datasetId, string createdBy)
        {
            var projectName = ProjectName.Create(name);
            var type = ProjectType.FromValue(projectType);
            var desc = Description.Create(description);

            return new Project(projectName, type, desc, datasetId, createdBy);
        }

        /// <summary>
        /// Changes the project type
        /// </summary>
        public void ChangeType(int newTypeValue, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(modifiedBy))
                throw new ValidationException(nameof(modifiedBy), "Modified by cannot be empty.");

            var oldType = Type;
            var newType = ProjectType.FromValue(newTypeValue);

            if (oldType.Value == newType.Value)
                return;

            Type = newType;
            UpdateAudit(modifiedBy);

            AddDomainEvent(new ProjectTypeChangedEvent(this, oldType, newType));
        }

        /// <summary>
        /// Adds a classification class to the project
        /// </summary>
        public void AddClass(ProjectClass projectClass)
        {
            if (projectClass == null)
                throw new ValidationException(nameof(projectClass), "Project class cannot be null.");

            if (_classes.Any(c => c.ClassIdx == projectClass.ClassIdx))
                throw new DuplicateEntityException(nameof(ProjectClass), $"Class with index {projectClass.ClassIdx}");

            _classes.Add(projectClass);
        }

        /// <summary>
        /// Removes a class from the project
        /// </summary>
        public void RemoveClass(int classId)
        {
            var projectClass = _classes.FirstOrDefault(c => c.Id == classId);
            if (projectClass == null)
                throw new EntityNotFoundException(nameof(ProjectClass), classId);

            _classes.Remove(projectClass);
        }

        /// <summary>
        /// Starts a new training run
        /// NOTE: This method is deprecated - use CreateTrainingCommand instead
        /// </summary>
        [Obsolete("Use CreateTrainingCommand instead")]
        public Training StartTraining(string trainingName)
        {
            if (!_classes.Any())
                throw new DomainOperationException("Cannot start training without project classes.");

            // Check if there's already an active training (Pending or Running)
            var hasActiveTraining = _trainings.Any(t =>
                t.Status.Value == TrainingStatus.Pending.Value ||
                t.Status.Value == TrainingStatus.Running.Value);

            if (hasActiveTraining)
                throw new DomainOperationException("Cannot start training while another training is active (Pending or Running).");

            var training = Training.Start(Id, trainingName);
            _trainings.Add(training);

            return training;
        }

        /// <summary>
        /// Updates the project name
        /// </summary>
        public void UpdateName(string newName, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(modifiedBy))
                throw new ValidationException(nameof(modifiedBy), "Modified by cannot be empty.");

            Name = ProjectName.Create(newName);
            UpdateAudit(modifiedBy);

            var changes = new Dictionary<string, object>
            {
                { nameof(Name), newName }
            };
            AddDomainEvent(new ProjectUpdatedEvent(this, changes));
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
            AddDomainEvent(new ProjectUpdatedEvent(this, changes));
        }

        /// <summary>
        /// Internal method to set the dataset relationship. Only called by Dataset.AddProject.
        /// </summary>
        internal void SetDataset(Dataset dataset)
        {
            if (dataset == null)
                throw new ValidationException(nameof(dataset), "Dataset cannot be null.");

            Dataset = dataset;
        }
    }
}
