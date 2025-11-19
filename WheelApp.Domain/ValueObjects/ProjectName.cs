using System.Text.RegularExpressions;
using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Validated project name value object
    /// </summary>
    public class ProjectName : ValueObject
    {
        public string Value { get; private set; }

        private ProjectName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a validated project name
        /// </summary>
        public static ProjectName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(ProjectName), "Project name cannot be empty.");

            if (value.Length > 50)
                throw new ValidationException(nameof(ProjectName), "Project name cannot exceed 50 characters.");

            // Prevent invalid file system characters
            var invalidChars = new Regex(@"[<>:""/\\|?*]");
            if (invalidChars.IsMatch(value))
                throw new ValidationException(nameof(ProjectName), "Project name contains invalid characters.");

            return new ProjectName(value);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(ProjectName projectName) => projectName.Value;
    }
}
