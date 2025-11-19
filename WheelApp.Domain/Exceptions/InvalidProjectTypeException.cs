namespace WheelApp.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid project type is specified
    /// </summary>
    public class InvalidProjectTypeException : ValidationException
    {
        public int AttemptedValue { get; }

        public InvalidProjectTypeException(int attemptedValue)
            : base($"Project type value '{attemptedValue}' is not valid. Valid values are: 0 (Classification), 1 (ObjectDetection), 2 (Segmentation), 3 (AnomalyDetection).")
        {
            AttemptedValue = attemptedValue;
        }
    }
}
