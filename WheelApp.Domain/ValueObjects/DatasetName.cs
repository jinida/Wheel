using System.Text.RegularExpressions;
using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Validated dataset name value object
    /// </summary>
    public class DatasetName : ValueObject
    {
        public string Value { get; private set; }

        private DatasetName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a validated dataset name
        /// </summary>
        public static DatasetName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(DatasetName), "Dataset name cannot be empty.");

            if (value.Length > 50)
                throw new ValidationException(nameof(DatasetName), "Dataset name cannot exceed 50 characters.");

            // Prevent invalid file system characters
            var invalidChars = new Regex(@"[<>:""/\\|?*]");
            if (invalidChars.IsMatch(value))
                throw new ValidationException(nameof(DatasetName), "Dataset name contains invalid characters.");

            return new DatasetName(value);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(DatasetName datasetName) => datasetName.Value;
    }
}
