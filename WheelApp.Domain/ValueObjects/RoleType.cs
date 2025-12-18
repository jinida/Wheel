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

        private static readonly RoleType _train = new(0, "Train");
        private static readonly RoleType _validation = new(1, "Validation");
        private static readonly RoleType _test = new(2, "Test");
        private static readonly RoleType _none = new(3, "None");

        public static RoleType Train => _train;
        public static RoleType Validation => _validation;
        public static RoleType Test => _test;
        public static RoleType None => _none;

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
