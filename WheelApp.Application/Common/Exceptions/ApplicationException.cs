namespace WheelApp.Application.Common.Exceptions;

/// <summary>
/// Base exception for application layer exceptions
/// </summary>
public class ApplicationException : Exception
{
    public ApplicationException()
        : base()
    {
    }

    public ApplicationException(string message)
        : base(message)
    {
    }

    public ApplicationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
