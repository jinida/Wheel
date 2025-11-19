namespace WheelApp.Domain.Common
{
    /// <summary>
    /// Functional result wrapper for operations
    /// Encapsulates success/failure state for railway-oriented programming
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public string Error { get; }

        private Result(bool isSuccess, T? value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result with a value
        /// </summary>
        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, string.Empty);
        }

        /// <summary>
        /// Creates a failed result with an error message
        /// </summary>
        public static Result<T> Failure(string error)
        {
            return new Result<T>(false, default, error);
        }

        /// <summary>
        /// Wraps an exception-throwing operation in a Result
        /// </summary>
        public static Result<T> Try(Func<T> operation)
        {
            try
            {
                return Success(operation());
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        /// <summary>
        /// Wraps an async exception-throwing operation in a Result
        /// </summary>
        public static async Task<Result<T>> TryAsync(Func<Task<T>> operation)
        {
            try
            {
                return Success(await operation());
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }
    }

    /// <summary>
    /// Result wrapper for operations without a return value
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static Result Success()
        {
            return new Result(true, string.Empty);
        }

        /// <summary>
        /// Creates a failed result with an error message
        /// </summary>
        public static Result Failure(string error)
        {
            return new Result(false, error);
        }

        /// <summary>
        /// Wraps an exception-throwing operation in a Result
        /// </summary>
        public static Result Try(Action operation)
        {
            try
            {
                operation();
                return Success();
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        /// <summary>
        /// Wraps an async exception-throwing operation in a Result
        /// </summary>
        public static async Task<Result> TryAsync(Func<Task> operation)
        {
            try
            {
                await operation();
                return Success();
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }
    }
}
