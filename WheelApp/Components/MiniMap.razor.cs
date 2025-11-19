using Microsoft.AspNetCore.Components;
using WheelApp.Services;

namespace WheelApp.Components
{
    /// <summary>
    /// MiniMap Component
    /// Shows viewport location when zoomed in
    /// Uses CanvasTransformService for transform state (no prop drilling)
    /// Subscribes to service events for automatic updates
    /// </summary>
    public partial class MiniMap : IDisposable
    {
        [Inject] private CanvasTransformService _canvasTransformService { get; set; } = default!;

        // Only these two parameters are needed - others come from service
        [Parameter]
        public string? ImageUrl { get; set; }

        [Parameter]
        public bool IsImageLoaded { get; set; }

        protected override void OnInitialized()
        {
            // Subscribe to service events for automatic updates
            _canvasTransformService.OnZoomChanged += HandleZoomChanged;
            _canvasTransformService.OnPanChanged += HandlePanChanged;
            _canvasTransformService.OnTransformReady += HandleTransformReady;
        }

        public void Dispose()
        {
            // Unsubscribe from service events
            _canvasTransformService.OnZoomChanged -= HandleZoomChanged;
            _canvasTransformService.OnPanChanged -= HandlePanChanged;
            _canvasTransformService.OnTransformReady -= HandleTransformReady;
        }

        private void HandleZoomChanged(double zoom)
        {
            // MiniMap needs to update when zoom changes
            InvokeAsync(StateHasChanged);
        }

        private void HandlePanChanged(double panX, double panY)
        {
            // MiniMap needs to update when pan changes
            InvokeAsync(StateHasChanged);
        }

        private void HandleTransformReady()
        {
            // MiniMap needs to update when transform is ready (e.g., new image loaded)
            InvokeAsync(StateHasChanged);
        }

        // All transform state comes from CanvasTransformService (no prop drilling)
        private double BaseScale => _canvasTransformService.BaseScale;
        private double Zoom => _canvasTransformService.Zoom;
        private double PanX => _canvasTransformService.PanX;
        private double PanY => _canvasTransformService.PanY;
        private double ViewportWidth => _canvasTransformService.ViewportWidth;
        private double ViewportHeight => _canvasTransformService.ViewportHeight;
        private int ImageWidth => _canvasTransformService.ImageWidth;
        private int ImageHeight => _canvasTransformService.ImageHeight;

        private const double MinimapSize = 150;

        /// <summary>
        /// Calculate minimap dimensions based on image aspect ratio
        /// The minimap fits the entire image within a 150x150 box while maintaining aspect ratio
        /// </summary>
        private (double width, double height) GetMinimapDimensions()
        {
            if (ImageWidth == 0 || ImageHeight == 0)
                return (MinimapSize, MinimapSize);

            var aspectRatio = (double)ImageWidth / ImageHeight;

            if (aspectRatio > 1)
            {
                // Wider than tall - width is 150, height is smaller
                return (MinimapSize, MinimapSize / aspectRatio);
            }
            else
            {
                // Taller than wide - height is 150, width is smaller
                return (MinimapSize * aspectRatio, MinimapSize);
            }
        }

        private double GetMinimapLeft()
        {
            if (ImageWidth == 0 || ViewportWidth == 0) return 0;

            var (minimapWidth, _) = GetMinimapDimensions();

            // With transform: translate(panX, panY) scale(baseScale * zoom) and transform-origin: 0 0
            // A point at image coords (imgX, 0) appears at screen position: panX + imgX * baseScale * zoom
            // The viewport shows the portion of the image from screenX=0 to screenX=viewportWidth

            // Find which image X coordinates are visible in the viewport
            // screenX = panX + imgX * (baseScale * zoom)
            // imgX = (screenX - panX) / (baseScale * zoom)

            var totalScale = BaseScale * Zoom;
            var viewportStartInImage = (0 - PanX) / totalScale;  // Left edge of viewport
            var viewportEndInImage = (ViewportWidth - PanX) / totalScale;  // Right edge of viewport

            // Clamp to image bounds
            viewportStartInImage = Math.Max(0, Math.Min(ImageWidth, viewportStartInImage));
            viewportEndInImage = Math.Max(0, Math.Min(ImageWidth, viewportEndInImage));

            var viewportWidthInImage = viewportEndInImage - viewportStartInImage;

            // Convert to minimap coordinates
            var minimapScale = minimapWidth / ImageWidth;
            var left = viewportStartInImage * minimapScale;
            var viewportWidthInMinimap = viewportWidthInImage * minimapScale;

            // Clamp to minimap bounds
            return Math.Max(0, Math.Min(minimapWidth - viewportWidthInMinimap, left));
        }

        private double GetMinimapTop()
        {
            if (ImageHeight == 0 || ViewportHeight == 0) return 0;

            var (_, minimapHeight) = GetMinimapDimensions();

            // With transform: translate(panX, panY) scale(baseScale * zoom) and transform-origin: 0 0
            // A point at image coords (0, imgY) appears at screen position: panY + imgY * baseScale * zoom
            // The viewport shows the portion of the image from screenY=0 to screenY=viewportHeight

            // Find which image Y coordinates are visible in the viewport
            // screenY = panY + imgY * (baseScale * zoom)
            // imgY = (screenY - panY) / (baseScale * zoom)

            var totalScale = BaseScale * Zoom;
            var viewportStartInImage = (0 - PanY) / totalScale;  // Top edge of viewport
            var viewportEndInImage = (ViewportHeight - PanY) / totalScale;  // Bottom edge of viewport

            // Clamp to image bounds
            viewportStartInImage = Math.Max(0, Math.Min(ImageHeight, viewportStartInImage));
            viewportEndInImage = Math.Max(0, Math.Min(ImageHeight, viewportEndInImage));

            var viewportHeightInImage = viewportEndInImage - viewportStartInImage;

            // Convert to minimap coordinates
            var minimapScale = minimapHeight / ImageHeight;
            var top = viewportStartInImage * minimapScale;
            var viewportHeightInMinimap = viewportHeightInImage * minimapScale;

            // Clamp to minimap bounds
            return Math.Max(0, Math.Min(minimapHeight - viewportHeightInMinimap, top));
        }

        private double GetMinimapWidth()
        {
            if (ImageWidth == 0 || ViewportWidth == 0) return MinimapSize;

            var (minimapWidth, _) = GetMinimapDimensions();

            var totalScale = BaseScale * Zoom;
            var viewportStartInImage = (0 - PanX) / totalScale;
            var viewportEndInImage = (ViewportWidth - PanX) / totalScale;

            viewportStartInImage = Math.Max(0, Math.Min(ImageWidth, viewportStartInImage));
            viewportEndInImage = Math.Max(0, Math.Min(ImageWidth, viewportEndInImage));

            var viewportWidthInImage = viewportEndInImage - viewportStartInImage;
            var minimapScale = minimapWidth / ImageWidth;

            return viewportWidthInImage * minimapScale;
        }

        private double GetMinimapHeight()
        {
            if (ImageHeight == 0 || ViewportHeight == 0) return MinimapSize;

            var (_, minimapHeight) = GetMinimapDimensions();

            var totalScale = BaseScale * Zoom;
            var viewportStartInImage = (0 - PanY) / totalScale;
            var viewportEndInImage = (ViewportHeight - PanY) / totalScale;

            viewportStartInImage = Math.Max(0, Math.Min(ImageHeight, viewportStartInImage));
            viewportEndInImage = Math.Max(0, Math.Min(ImageHeight, viewportEndInImage));

            var viewportHeightInImage = viewportEndInImage - viewportStartInImage;
            var minimapScale = minimapHeight / ImageHeight;

            return viewportHeightInImage * minimapScale;
        }
    }
}