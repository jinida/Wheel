namespace WheelApp.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid domain operation is attempted
    /// </summary>
    public class DomainOperationException : DomainException
    {
        public DomainOperationException(string message) : base(message)
        {
        }

        public DomainOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
