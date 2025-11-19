namespace WheelApp.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid training status transition is attempted
    /// </summary>
    public class InvalidTrainingStatusException : ValidationException
    {
        public int CurrentStatus { get; }
        public int AttemptedStatus { get; }

        public InvalidTrainingStatusException(int attemptedStatus)
            : base($"Training status value '{attemptedStatus}' is not valid. Valid values are: 0 (Pending), 1 (Running), 2 (Completed), 3 (Failed).")
        {
            AttemptedStatus = attemptedStatus;
            CurrentStatus = -1;
        }

        public InvalidTrainingStatusException(int currentStatus, int attemptedStatus)
            : base($"Cannot transition training status from '{currentStatus}' to '{attemptedStatus}'.")
        {
            CurrentStatus = currentStatus;
            AttemptedStatus = attemptedStatus;
        }
    }
}
