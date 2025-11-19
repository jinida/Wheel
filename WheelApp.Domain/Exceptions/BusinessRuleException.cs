namespace WheelApp.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule violation occurs
    /// </summary>
    public class BusinessRuleException : DomainException
    {
        public BusinessRuleException(string message) : base(message)
        {
        }

        public BusinessRuleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
