using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Type-safe project type enumeration
    /// </summary>
    public class ProjectType : ValueObject
    {
        public int Value { get; init; }
        public string Name { get; init; }

        private ProjectType(int value, string name)
        {
            Value = value;
            Name = name;
        }

        public static ProjectType Classification => new(0, "Classification");
        public static ProjectType ObjectDetection => new(1, "Object Detection");
        public static ProjectType Segmentation => new(2, "Segmentation");
        public static ProjectType AnomalyDetection => new(3, "Anomaly Detection");

        private static readonly Dictionary<int, Func<ProjectType>> _factory = new()
        {
            [0] = () => Classification,
            [1] = () => ObjectDetection,
            [2] = () => Segmentation,
            [3] = () => AnomalyDetection
        };

        /// <summary>
        /// Creates a ProjectType from an integer value
        /// </summary>
        public static ProjectType FromValue(int value)
        {
            if (_factory.TryGetValue(value, out var factory))
            {
                return factory();
            }

            throw new InvalidProjectTypeException(value);
        }

        /// <summary>
        /// Gets all valid project types
        /// </summary>
        public static IEnumerable<ProjectType> GetAll()
        {
            yield return Classification;
            yield return ObjectDetection;
            yield return Segmentation;
            yield return AnomalyDetection;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Name;

        public static implicit operator int(ProjectType projectType) => projectType.Value;
    }
}
