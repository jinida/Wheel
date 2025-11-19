using System.Text.RegularExpressions;
using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Validated file path value object with security checks
    /// </summary>
    public class FilePath : ValueObject
    {
        public string Value { get; private set; }

        private FilePath(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a validated file path with security validation
        /// </summary>
        public static FilePath Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(FilePath), "File path cannot be empty.");

            if (value.Length > 512)
                throw new ValidationException(nameof(FilePath), "File path cannot exceed 512 characters.");

            // Prevent path traversal attacks
            if (value.Contains("..") || value.Contains("//") || value.Contains("\\\\"))
                throw new ValidationException(nameof(FilePath), "File path contains invalid sequences.");

            // Check for dangerous characters
            var dangerousChars = new Regex(@"[<>:""|?*\x00-\x1F]");
            if (dangerousChars.IsMatch(value))
                throw new ValidationException(nameof(FilePath), "File path contains invalid characters.");

            return new FilePath(value);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(FilePath filePath) => filePath.Value;
    }
}
