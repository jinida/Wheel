using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// ProjectClass entity representing a classification class within a project
    /// </summary>
    public class ProjectClass : Entity
    {
        private readonly List<Annotation> _annotations = new();

        public int ProjectId { get; private set; }
        public ClassIndex ClassIdx { get; private set; }
        public string Name { get; private set; }
        public ColorCode Color { get; private set; }

        // Navigation properties
        public Project? Project { get; private set; }  // EF Core navigation property
        public IReadOnlyCollection<Annotation> Annotations => _annotations.AsReadOnly();

        private ProjectClass() { }  // For EF Core

        private ProjectClass(int projectId, ClassIndex classIdx, string name, ColorCode color)
        {
            ProjectId = projectId;
            ClassIdx = classIdx;
            Name = name;
            Color = color;
        }

        /// <summary>
        /// Factory method to create a new project class
        /// </summary>
        public static ProjectClass Create(int projectId, int classIdx, string name, string color)
        {
            if (projectId <= 0)
                throw new ValidationException(nameof(projectId), "Project ID must be positive.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException(nameof(name), "Class name cannot be empty.");

            if (name.Length > 30)
                throw new ValidationException(nameof(name), "Class name cannot exceed 30 characters.");

            var idx = ClassIndex.Create(classIdx);
            var colorCode = ColorCode.Create(color);

            return new ProjectClass(projectId, idx, name, colorCode);
        }

        /// <summary>
        /// Updates the class color
        /// </summary>
        public void UpdateColor(string color)
        {
            Color = ColorCode.Create(color);
        }

        /// <summary>
        /// Updates the class name
        /// </summary>
        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException(nameof(name), "Class name cannot be empty.");

            if (name.Length > 30)
                throw new ValidationException(nameof(name), "Class name cannot exceed 30 characters.");

            Name = name;
        }

        /// <summary>
        /// Updates the class index
        /// </summary>
        public void UpdateClassIdx(int classIdx)
        {
            ClassIdx = ClassIndex.Create(classIdx);
        }
    }
}
