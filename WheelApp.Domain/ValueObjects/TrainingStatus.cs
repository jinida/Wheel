using WheelApp.Domain.Common;
using WheelApp.Domain.Exceptions;

namespace WheelApp.Domain.ValueObjects
{
    /// <summary>
    /// Type-safe training status enumeration with state transition validation
    /// </summary>
    public class TrainingStatus : ValueObject
    {
        public int Value { get; private set; }
        public string Name { get; private set; }

        // Configuration-based state transitions (Open/Closed Principle compliant)
        private static readonly Dictionary<int, int[]> AllowedTransitions = new()
        {
            [0] = new[] { 1 },       
            [1] = new[] { 2, 3 },    
            [2] = Array.Empty<int>(),
            [3] = Array.Empty<int>() 
        };

        private TrainingStatus(int value, string name)
        {
            Value = value;
            Name = name;
        }

        public static TrainingStatus Pending => new(0, "Pending");
        public static TrainingStatus Running => new(1, "Running");
        public static TrainingStatus Completed => new(2, "Completed");
        public static TrainingStatus Failed => new(3, "Failed");
        
        private static readonly Dictionary<int, Func<TrainingStatus>> _factory = new()
        {
            [0] = () => Pending,
            [1] = () => Running,
            [2] = () => Completed,
            [3] = () => Failed
        };
        /// <summary>
        /// Creates a TrainingStatus from an integer value
        /// </summary>
        public static TrainingStatus FromValue(int value)
        {
            if (_factory.TryGetValue(value, out var factory))
            {
                return factory();
            }
            
            throw new InvalidTrainingStatusException(value);
        }

        /// <summary>
        /// Gets all valid training statuses
        /// </summary>
        public static IEnumerable<TrainingStatus> GetAll()
        {
            yield return Pending;
            yield return Running;
            yield return Completed;
            yield return Failed;
        }

        /// <summary>
        /// Checks if transition to target status is valid using configuration-based approach
        /// </summary>
        public bool CanTransitionTo(TrainingStatus target)
        {
            return AllowedTransitions.TryGetValue(Value, out var allowedTargets)
                && allowedTargets.Contains(target.Value);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Name;

        public static implicit operator int(TrainingStatus trainingStatus) => trainingStatus.Value;
    }
}
