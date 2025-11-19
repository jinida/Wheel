using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Type-safe role type enumeration for training data splits
    /// </summary>
    public class RoleType : ValueObject
    {
        public int Value { get; private set; }
        public string Name { get; private set; }

        private RoleType(int value, string name)
        {
            Value = value;
            Name = name;
        }

        public static RoleType Train => new(0, "Train");
        public static RoleType Validation => new(1, "Validation");
        public static RoleType Test => new(2, "Test");
        public static RoleType None => new(3, "None");

        /// <summary>
        /// Creates a RoleType from an integer value
        /// </summary>
        public static RoleType FromValue(int value)
        {
            return value switch
            {
                0 => Train,
                1 => Validation,
                2 => Test,
                3 => None,
                _ => throw new ValidationException(nameof(RoleType), $"Invalid role type value: {value}")
            };
        }

        /// <summary>
        /// Gets all valid role types
        /// </summary>
        public static IEnumerable<RoleType> GetAll()
        {
            yield return Train;
            yield return Validation;
            yield return Test;
            yield return None;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Name;

        public static implicit operator int(RoleType roleType) => roleType.Value;
    }
}
