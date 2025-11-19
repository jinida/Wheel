using Microsoft.JSInterop;
using WheelApp.Extensions;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Helper for keyboard shortcuts and navigation
    /// Phase 2 Refactoring - Lightweight helper without DI registration
    /// Only requires IJSRuntime which is passed directly from component
    /// </summary>
    public class KeyboardListenerHelper : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private DotNetObjectReference<object>? _componentRef;

        public KeyboardListenerHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Initializes keyboard listeners via JSInterop
        /// Creates DotNetObjectReference for callbacks
        /// </summary>
        /// <param name="component">The Blazor component (Project.razor.cs or Evaluate.razor.cs) that will receive callbacks</param>
        /// <param name="gridContainerId">The ID of the grid container (default: "image-grid-container")</param>
        public async Task InitializeKeyboardListenersAsync(object component, string gridContainerId = "image-grid-container")
        {
            // Create DotNetObjectReference for JSInvokable callbacks
            _componentRef = DotNetObjectReference.Create(component);

            // Use JSRuntimeExtensions to eliminate duplicate try-catch
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.initializeGridKeyListener",
                gridContainerId, _componentRef);
        }

        /// <summary>
        /// Cleans up keyboard listeners and releases JSInterop resources
        /// </summary>
        public async Task CleanupKeyboardListenersAsync()
        {
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.cleanupGlobalCanvasShortcuts");
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.cleanupGridKeyListener");
        }

        /// <summary>
        /// IAsyncDisposable implementation for proper resource cleanup
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await CleanupKeyboardListenersAsync();
            _componentRef?.Dispose();
        }
    }
}
