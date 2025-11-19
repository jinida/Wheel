using Microsoft.JSInterop;
using WheelApp.Extensions;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Helper for UI initialization and state management (Grid, Scroll, Resize, Animation)
    /// Phase 2 Refactoring - Lightweight helper without DI registration
    /// Only requires IJSRuntime which is passed directly from component
    /// </summary>
    public class UIEnhancementHelper
    {
        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Set of image row IDs that are currently animating
        /// Used for visual feedback during bulk operations
        /// </summary>
        public HashSet<int> AnimatingRows { get; } = new();

        /// <summary>
        /// Indicates whether bulk processing is currently in progress
        /// Used to disable UI during batch operations
        /// </summary>
        public bool IsBulkProcessing { get; set; }

        public UIEnhancementHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Initializes all UI enhancements (resizable panels, scrollbar, column resizing)
        /// Should be called in OnAfterRenderAsync(firstRender)
        /// </summary>
        /// <param name="gridContainerId">The ID of the grid container (default: "image-grid-container")</param>
        public async Task InitializeUIEnhancementsAsync(string gridContainerId = "image-grid-container")
        {
            // Use JSRuntimeExtensions to eliminate duplicate try-catch
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.makeResizable", "resizable-container");
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.initializeCustomScrollbar", gridContainerId);
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.initializeColumnResizing", gridContainerId);
        }

        /// <summary>
        /// Scrolls to a specific image in the grid and highlights it
        /// </summary>
        /// <param name="imageId">The ID of the image to scroll to</param>
        /// <param name="gridContainerId">The ID of the grid container (default: "image-grid-container")</param>
        public async Task ScrollToImageAsync(int imageId, string gridContainerId = "image-grid-container")
        {
            // Use JSRuntimeExtensions to eliminate duplicate try-catch
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.scrollToElement",
                gridContainerId,
                $"#row-{imageId} .grid-cell");
        }

        /// <summary>
        /// Triggers grid UI update
        /// Note: In Blazor, this is typically handled by StateHasChanged() in the component
        /// This method is a placeholder for potential future JS-based grid updates
        /// </summary>
        public Task UpdateGridUIAsync()
        {
            // Currently no JS-specific grid update is needed
            // Grid updates are handled by Blazor's change detection
            return Task.CompletedTask;
        }
    }
}
