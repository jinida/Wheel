using System.Text.RegularExpressions;
using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Validated hex color code value object
    /// </summary>
    public class ColorCode : ValueObject
    {
        public string Value { get; private set; }

        private ColorCode(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a validated color code in #RRGGBB format
        /// </summary>
        public static ColorCode Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException(nameof(ColorCode), "Color code cannot be empty.");

            // Validate hex color format (#RRGGBB)
            var hexColorPattern = new Regex(@"^#[0-9A-Fa-f]{6}$");
            if (!hexColorPattern.IsMatch(value))
                throw new ValidationException(nameof(ColorCode), "Color code must be in #RRGGBB format (e.g., #FF5733).");

            return new ColorCode(value.ToUpper());
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(ColorCode colorCode) => colorCode.Value;
    }
}
