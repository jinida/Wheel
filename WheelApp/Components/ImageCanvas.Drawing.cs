using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;
using WheelApp.Service;
using WheelApp.Services;

namespace WheelApp.Components
{
    /// <summary>
    /// ImageCanvas - Drawing operations (BoundingBox, Segmentation)
    /// Handles creation of new annotations during drawing mode
    /// </summary>
    public partial class ImageCanvas
    {
        private async Task StartDrawing(MouseEventArgs e)
        {
            // Disable drawing in evaluation/view-only mode
            if (DisableLabeling) return;

            // Prevent drawing if no class is selected
            if (_drawingToolService.SelectedClass == null)
            {
                return;
            }

            try
            {
                var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (coords != null && coords.Length == 2)
                {
                    if (_currentMode == LabelMode.BoundingBox)
                    {
                        var clampedPoint = ClampPointToImage(coords[0], coords[1]);

                        if (_drawingToolService.SelectedClass == null) return;

                        _currentDrawing = new AnnotationDto
                        {
                            Id = 0,  // Will be assigned by database
                            classDto = _drawingToolService.SelectedClass,
                            Information = new List<Point2f>
                            {
                                clampedPoint,
                                new Point2f(clampedPoint.X, clampedPoint.Y)
                            },
                            CreatedAt = DateTime.Now
                        };
                    }
                    else if (_currentMode == LabelMode.Segmentation)
                    {
                        if (_currentDrawing == null)
                        {
                            _currentDrawing = new AnnotationDto
                            {
                                Id = 0,  // Will be assigned by database
                                classDto = _drawingToolService.SelectedClass,
                                Information = new List<Point2f>
                                {
                                    new Point2f(coords[0], coords[1])
                                },
                                CreatedAt = DateTime.Now
                            };
                        }
                        else
                        {
                            // Check if clicking near first point to close polygon
                            var firstPoint = _currentDrawing.Information[0];
                            var distance = ImageCanvas.Helpers.CalculateDistance(coords[0], coords[1], firstPoint.X, firstPoint.Y);

                            // Zoom-aware threshold: more forgiving threshold for easier closing
                            var threshold = 25.0 / _canvasTransformService.Zoom;

                            if (distance < threshold && _currentDrawing.Information.Count >= 3)
                            {
                                // Close and finish polygon - create a copy to avoid reference sharing
                                var completedAnnotation = new AnnotationDto
                                {
                                    Id = _currentDrawing.Id,
                                    classDto = _currentDrawing.classDto,
                                    Information = _currentDrawing.Information.Select(p => new Point2f (p.X, p.Y)).ToList(),
                                    CreatedAt = _currentDrawing.CreatedAt
                                };
                                _annotations.Add(completedAnnotation);

                                await OnSegmentationAdded.InvokeAsync(completedAnnotation);

                                // Auto-select the newly created polygon
                                _annotationService.SelectedAnnotationIds.Clear();
                                _annotationService.SelectedAnnotationIds.Add(completedAnnotation.Id);
                                _annotationService.SelectMultiple(new HashSet<int>(_annotationService.SelectedAnnotationIds));
                                Logger.LogInformation("[ImageCanvas] New Polygon created (closed) with ID {Id}, auto-selected", completedAnnotation.Id);

                                _currentDrawing = null;
                                _currentMousePosition = null;
                                _isNearFirstPoint = false;
                            }
                            else
                            {
                                // Add new point
                                _currentDrawing.Information.Add(new Point2f(coords[0], coords[1]));
                            }
                        }
                    }

                    StateHasChanged();
                }
            }
            catch { }
        }

        private async Task UpdateDrawing(MouseEventArgs e)
        {
            if (_currentDrawing == null) return;

            try
            {
                var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (coords != null && coords.Length == 2)
                {
                    if (_currentMode == LabelMode.BoundingBox)
                    {
                        // Update second point with clamping
                        var clampedPoint = ClampPointToImage(coords[0], coords[1]);
                        _currentDrawing.Information[^1] = clampedPoint;
                        StateHasChanged();
                    }
                    else if (_currentMode == LabelMode.Segmentation)
                    {
                        // Update current mouse position for preview line
                        _currentMousePosition = new Point2f(coords[0], coords[1]);

                        // Check if near first point for closing indicator
                        if (_currentDrawing.Information.Count >= 3)
                        {
                            var firstPoint = _currentDrawing.Information[0];
                            var distance = ImageCanvas.Helpers.CalculateDistance(coords[0], coords[1], firstPoint.X, firstPoint.Y);

                            // Zoom-aware threshold: more forgiving for easier closing
                            var threshold = 12.0 / _canvasTransformService.Zoom;
                            _isNearFirstPoint = distance < threshold;
                        }
                        else
                        {
                            _isNearFirstPoint = false;
                        }

                        StateHasChanged();
                    }
                }
            }
            catch { }
        }

        private async Task FinishBoundingBox(MouseEventArgs e)
        {
            if (_currentDrawing == null || _isFinishingBbox) return;

            _isFinishingBbox = true;

            try
            {
                var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (coords != null && coords.Length == 2)
                {
                    var clampedPoint = ClampPointToImage(coords[0], coords[1]);
                    _currentDrawing.Information[^1] = clampedPoint;

                    // Only add if box has some size
                    var width = Math.Abs(_currentDrawing.Information[1].X - _currentDrawing.Information[0].X);
                    var height = Math.Abs(_currentDrawing.Information[1].Y - _currentDrawing.Information[0].Y);

                    if (width > 5 && height > 5)
                    {
                        // Normalize bbox points so Point[0] is always top-left and Point[1] is always bottom-right
                        // This ensures consistent behavior for resize and move operations
                        // Calculate once and reuse for both updating points and database operations
                        var (x1, y1, x2, y2) = ImageCanvas.Helpers.GetBboxCoordinates(_currentDrawing.Information[0], _currentDrawing.Information[1]);

                        _currentDrawing.Information[0] = new Point2f((float)x1, (float)y1);
                        _currentDrawing.Information[1] = new Point2f((float)x2, (float)y2);

                        var completedAnnotation = new AnnotationDto
                        {
                            Id = _currentDrawing.Id,
                            classDto = _currentDrawing.classDto,
                            Information = new List<Point2f>
                            {
                                new Point2f((float)x1, (float)y1),
                                new Point2f((float)x2, (float)y2)
                            },
                            CreatedAt = _currentDrawing.CreatedAt
                        };
                        _annotations.Add(completedAnnotation);

                        // Save to database first - this updates completedAnnotation.Id with the real database ID
                        await OnBboxAdded.InvokeAsync(completedAnnotation);

                        // Now select using the real ID from database
                        _annotationService.SelectedAnnotationIds.Clear();
                        _annotationService.SelectedAnnotationIds.Add(completedAnnotation.Id);
                        _annotationService.SelectMultiple(new HashSet<int>(_annotationService.SelectedAnnotationIds));
                        Logger.LogInformation("[ImageCanvas] New BBOX created with ID {Id}, auto-selected", completedAnnotation.Id);
                    }

                    _currentDrawing = null;
                    StateHasChanged();
                }
            }
            catch
            {
                _currentDrawing = null;
                StateHasChanged();
            }
            finally
            {
                _isFinishingBbox = false;
            }
        }
    }
}
