using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Validated class index value object
    /// </summary>
    public class ClassIndex : ValueObject
    {
        public int Value { get; private set; }

        private ClassIndex(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a validated class index
        /// </summary>
        public static ClassIndex Create(int value)
        {
            if (value < 0)
                throw new ValidationException(nameof(ClassIndex), "Class index cannot be negative.");

            return new ClassIndex(value);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator int(ClassIndex classIndex) => classIndex.Value;
    }
}
