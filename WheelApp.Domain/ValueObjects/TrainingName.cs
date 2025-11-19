using System.Text.RegularExpressions;
using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Validated training name value object
    /// </summary>
    public class TrainingName : ValueObject
    {
        public string Value { get; private set; }

        private TrainingName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a validated training name
        /// </summary>
        public static TrainingName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(TrainingName), "Training name cannot be empty.");

            if (value.Length > 200)
                throw new ValidationException(nameof(TrainingName), "Training name cannot exceed 200 characters.");

            // Prevent invalid file system characters
            var invalidChars = new Regex(@"[<>:""/\\|?*]");
            if (invalidChars.IsMatch(value))
                throw new ValidationException(nameof(TrainingName), "Training name contains invalid characters.");

            return new TrainingName(value);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(TrainingName trainingName) => trainingName.Value;
    }
}
