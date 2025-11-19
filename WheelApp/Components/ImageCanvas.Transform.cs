using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Service;
using WheelApp.Services;

namespace WheelApp.Components
{
    /// <summary>
    /// ImageCanvas - Transform operations (Zoom, Pan, Image Adjustments)
    /// Handles all viewport transformation logic including zoom, pan, and image adjustments
    /// </summary>
    public partial class ImageCanvas
    {
        private string GetGammaFilter()
        {
            // Use SVG filter for gamma correction
            var gammaValue = _gamma.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var svgContent = "<svg xmlns=\"http://www.w3.org/2000/svg\">" +
                            "<filter id=\"gamma\">" +
                            "<feComponentTransfer>" +
                            $"<feFuncR type=\"gamma\" exponent=\"{gammaValue}\"/>" +
                            $"<feFuncG type=\"gamma\" exponent=\"{gammaValue}\"/>" +
                            $"<feFuncB type=\"gamma\" exponent=\"{gammaValue}\"/>" +
                            "</feComponentTransfer>" +
                            "</filter>" +
                            "</svg>";
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svgContent));
            return $"url('data:image/svg+xml;base64,{encoded}#gamma')";
        }

        private async void ZoomIn()
        {
            await ZoomTowardsCenter(_canvasTransformService.Zoom + 0.25);
        }

        private async void ZoomOut()
        {
            await ZoomTowardsCenter(_canvasTransformService.Zoom - 0.25);
        }

        private async void SetZoom(double zoom)
        {
            await ZoomTowardsCenter(zoom);
        }

        private async Task ZoomTowardsCenter(double targetZoom)
        {
            if (!_imageLoaded) return;

            var oldZoom = _canvasTransformService.Zoom;
            var newZoom = Math.Max(0.25, Math.Min(5.0, targetZoom));

            if (Math.Abs(oldZoom - newZoom) < 0.001) return; // No significant change

            try
            {
                // Get viewport dimensions to find center point
                var viewportDimensions = await JSRuntime.InvokeAsync<double[]>("getElementDimensions", _viewport);
                if (viewportDimensions != null && viewportDimensions.Length >= 2)
                {
                    var viewportWidth = viewportDimensions[0];
                    var viewportHeight = viewportDimensions[1];

                    // Zoom towards viewport center
                    var centerX = viewportWidth / 2;
                    var centerY = viewportHeight / 2;

                    // Find which image point is at the viewport center (before zoom)
                    var imgX = (centerX - _canvasTransformService.PanX) / (_canvasTransformService.BaseScale * oldZoom);
                    var imgY = (centerY - _canvasTransformService.PanY) / (_canvasTransformService.BaseScale * oldZoom);

                    // Update zoom
                    _canvasTransformService.SetZoom(newZoom);

                    // Adjust pan so the same image point stays at center
                    var newPanX = centerX - imgX * (_canvasTransformService.BaseScale * newZoom);
                    var newPanY = centerY - imgY * (_canvasTransformService.BaseScale * newZoom);
                    _canvasTransformService.SetPan(newPanX, newPanY);
                }
                else
                {
                    // Fallback: just change zoom
                    _canvasTransformService.SetZoom(newZoom);
                }
            }
            catch
            {
                // Fallback: just change zoom
                _canvasTransformService.SetZoom(newZoom);
            }

            StateHasChanged();
        }

        private void ResetZoom()
        {
            // Service method handles zoom reset and recentering
            _canvasTransformService.ResetZoom();
            StateHasChanged();
        }

        private void ResetImageAdjustments()
        {
            // Reset only image adjustment values (brightness, gamma, contrast)
            // Do NOT reset zoom and pan - those are view controls, not image adjustments
            _brightness = 100;
            _gamma = 1.0;
            _contrast = 100;
        }

        private async Task HandleWheel(WheelEventArgs e)
        {
            if (!_imageLoaded) return;

            // Calculate new zoom level
            var oldZoom = _canvasTransformService.Zoom;
            var newZoom = e.DeltaY < 0
                ? Math.Min(_canvasTransformService.Zoom + 0.1, 5.0)
                : Math.Max(_canvasTransformService.Zoom - 0.1, 0.25);

            if (Math.Abs(oldZoom - newZoom) < 0.001) return; // No significant change

            try
            {
                // Get viewport rect to calculate mouse position
                var viewportRect = await JSRuntime.InvokeAsync<double[]>("getElementBoundingRect", _viewport);
                if (viewportRect == null || viewportRect.Length < 6)
                {
                    // Fallback: just change zoom without adjusting pan
                    _canvasTransformService.SetZoom(newZoom);
                    StateHasChanged();
                    return;
                }

                // Mouse position relative to viewport (top-left corner)
                var mouseX = e.ClientX - viewportRect[0];
                var mouseY = e.ClientY - viewportRect[1];

                var imgX = (mouseX - _canvasTransformService.PanX) / (_canvasTransformService.BaseScale * oldZoom);
                var imgY = (mouseY - _canvasTransformService.PanY) / (_canvasTransformService.BaseScale * oldZoom);

                // Update zoom
                _canvasTransformService.SetZoom(newZoom);
                var newPanX = mouseX - imgX * (_canvasTransformService.BaseScale * newZoom);
                var newPanY = mouseY - imgY * (_canvasTransformService.BaseScale * newZoom);
                _canvasTransformService.SetPan(newPanX, newPanY);

                StateHasChanged();
            }
            catch
            {
                // Fallback: just change zoom
                _canvasTransformService.SetZoom(newZoom);
                StateHasChanged();
            }
        }

        private void StartPan(MouseEventArgs e)
        {
            if (e.Button == 0) // Only left mouse button
            {
                _canvasTransformService.StartPan(e.ClientX, e.ClientY);
                StateHasChanged(); // Update cursor
            }
        }

        private void DoPan(MouseEventArgs e)
        {
            if (_canvasTransformService.IsPanning)
            {
                var deltaX = e.ClientX - _canvasTransformService.LastMouseX;
                var deltaY = e.ClientY - _canvasTransformService.LastMouseY;

                // With translate before scale, pan values are in screen pixels
                var newPanX = _canvasTransformService.PanX + deltaX;
                var newPanY = _canvasTransformService.PanY + deltaY;
                _canvasTransformService.SetPan(newPanX, newPanY);

                _canvasTransformService.StartPan(e.ClientX, e.ClientY);
                StateHasChanged(); // Update display
            }
        }

        private void EndPan(MouseEventArgs e)
        {
            if (_canvasTransformService.IsPanning)
            {
                _canvasTransformService.EndPan();
                StateHasChanged(); // Update cursor
            }
        }

        private async Task HandleMouseMove(MouseEventArgs e)
        {
            if (!_imageLoaded || _canvasTransformService.IsPanning) return; // Don't track coordinates while panning

            try
            {
                // Get viewport bounding rect to calculate accurate mouse position
                var viewportRect = await JSRuntime.InvokeAsync<double[]>("getElementBoundingRect", _viewport);
                if (viewportRect != null && viewportRect.Length >= 4)
                {
                    // Track mouse position relative to viewport for zoom calculations
                    _lastMouseScreenX = e.ClientX - viewportRect[0]; // clientX - left
                    _lastMouseScreenY = e.ClientY - viewportRect[1]; // clientY - top
                }

                // Get image coordinates accounting for zoom and pan
                var result = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (result != null && result.Length == 2)
                {
                    _mouseX = result[0];
                    _mouseY = result[1];
                    _showCoordinates = true;
                    StateHasChanged();
                }
            }
            catch
            {
                // Fallback to simple offset if JS fails
                _lastMouseScreenX = e.OffsetX;
                _lastMouseScreenY = e.OffsetY;
                _mouseX = (int)e.OffsetX;
                _mouseY = (int)e.OffsetY;
                _showCoordinates = true;
            }
        }

        private void HandleMouseLeave(MouseEventArgs e)
        {
            _showCoordinates = false;
            if (_canvasTransformService.IsPanning)
            {
                _canvasTransformService.EndPan();
                StateHasChanged();
            }
        }

        private string GetCursor()
        {
            if (_currentMode == LabelMode.Select)
            {
                return "default";
            }
            else if (_currentMode == LabelMode.Move)
            {
                return _canvasTransformService.IsPanning ? "grabbing" : "grab";
            }
            else if (_currentMode == LabelMode.BoundingBox)
            {
                return "crosshair";
            }
            else if (_currentMode == LabelMode.Segmentation)
            {
                return "crosshair";
            }
            return "default";
        }
    }
}
