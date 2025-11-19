using Microsoft.JSInterop;

namespace WheelApp.Extensions
{
    /// <summary>
    /// Extension methods for IJSRuntime
    /// Provides safe JSInterop invocation with automatic error handling
    /// Eliminates 8+ duplicate try-catch blocks across coordinators
    /// </summary>
    public static class JSRuntimeExtensions
    {
        /// <summary>
        /// Attempts to invoke a JavaScript function without throwing exceptions
        /// Returns true if successful, false if JS interop is unavailable
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime</param>
        /// <param name="identifier">The JavaScript function identifier</param>
        /// <param name="args">Arguments to pass to the JavaScript function</param>
        /// <returns>True if invocation succeeded, false otherwise</returns>
        public static async Task<bool> TryInvokeVoidAsync(
            this IJSRuntime jsRuntime,
            string identifier,
            params object?[]? args)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync(identifier, args);
                return true;
            }
            catch (Exception)
            {
                // Silently fail if JS interop is not available
                // This can happen during prerendering or testing
                return false;
            }
        }

        /// <summary>
        /// Attempts to invoke a JavaScript function with a return value
        /// Returns (true, result) if successful, (false, default) if failed
        /// </summary>
        public static async Task<(bool Success, T? Result)> TryInvokeAsync<T>(
            this IJSRuntime jsRuntime,
            string identifier,
            params object?[]? args)
        {
            try
            {
                var result = await jsRuntime.InvokeAsync<T>(identifier, args);
                return (true, result);
            }
            catch (Exception)
            {
                // Silently fail if JS interop is not available
                return (false, default);
            }
        }
    }
}
