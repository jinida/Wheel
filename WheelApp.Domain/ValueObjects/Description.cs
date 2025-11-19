using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Validated description text value object
    /// </summary>
    public class Description : ValueObject
    {
        public string? Value { get; private set; }

        private Description(string? value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a validated description
        /// </summary>
        public static Description Create(string? value)
        {
            if (value != null && value.Length > 255)
                throw new ValidationException(nameof(Description), "Description cannot exceed 255 characters.");

            return new Description(value);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value ?? string.Empty;

        public static implicit operator string?(Description description) => description.Value;
    }
}
