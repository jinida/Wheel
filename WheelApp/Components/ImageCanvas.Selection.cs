using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;
using WheelApp.Service;
using WheelApp.Services;

namespace WheelApp.Components
{
    /// <summary>
    /// ImageCanvas - Selection and Editing operations
    /// Handles annotation selection, resizing, moving, and point manipulation
    /// </summary>
    public partial class ImageCanvas
    {
        private async Task HandleAnnotationMouseDown(int annotationId, MouseEventArgs e)
        {
            Logger.LogInformation("[ImageCanvas] HandleAnnotationMouseDown called - AnnotationId: {AnnotationId}, CtrlKey: {CtrlKey}", annotationId, e.CtrlKey);
            Logger.LogInformation("[ImageCanvas] Before - _annotationService.SelectedAnnotationIds: [{Ids}]", string.Join(", ", _annotationService.SelectedAnnotationIds));

            // Disable annotation interactions in evaluation/view-only mode
            if (DisableLabeling) return;

            // Only allow selection in Select mode
            if (_currentMode != LabelMode.Select) return;

            // Create new selection set based on user action
            HashSet<int> newSelection;
            if (e.CtrlKey)
            {
                // Ctrl+Click: Toggle annotation in selection
                newSelection = new HashSet<int>(_annotationService.SelectedAnnotationIds);
                if (newSelection.Contains(annotationId))
                {
                    newSelection.Remove(annotationId);
                    Logger.LogInformation("[ImageCanvas] Ctrl+MouseDown: Removed {AnnotationId} from selection", annotationId);
                }
                else
                {
                    newSelection.Add(annotationId);
                    Logger.LogInformation("[ImageCanvas] Ctrl+MouseDown: Added {AnnotationId} to selection", annotationId);
                }
            }
            else
            {
                // Regular click: Select only this annotation
                newSelection = new HashSet<int> { annotationId };
                Logger.LogInformation("[ImageCanvas] Regular mouseDown: Selected only {AnnotationId}", annotationId);
            }

            // Update service with new selection
            _annotationService.SelectMultiple(newSelection);
            Logger.LogInformation("[ImageCanvas] After - _annotationService.SelectedAnnotationIds: [{Ids}]", string.Join(", ", _annotationService.SelectedAnnotationIds));
            StateHasChanged();

            // Second: Prepare for potential move operation
            await StartMoveAnnotation(annotationId, e);
        }

        private void StartResizeBBox(int annotationId, ResizeHandle handle, MouseEventArgs e)
        {
            // Disable in evaluation/view-only mode
            if (DisableLabeling) return;

            if (_currentMode != LabelMode.Select) return;

            _isResizing = true;
            _resizingAnnotationId = annotationId;
            _resizeHandle = handle;

            // Only modify selection if NOT using Ctrl (let HandleAnnotationClick handle Ctrl+Click)
            if (!e.CtrlKey && !_annotationService.SelectedAnnotationIds.Contains(annotationId))
            {
                // Regular click: clear selection and select only this annotation
                var newSelection = new HashSet<int> { annotationId };
                _annotationService.SelectMultiple(newSelection);
            }

            StateHasChanged();
        }

        private void StartMovePolygonPoint(int annotationId, int pointIndex, MouseEventArgs e)
        {
            // Disable in evaluation/view-only mode
            if (DisableLabeling) return;

            if (_currentMode != LabelMode.Select) return;

            _isMovingPoint = true;
            _movingPointAnnotationId = annotationId;
            _movingPointIndex = pointIndex;

            // Only modify selection if NOT using Ctrl (let HandleAnnotationClick handle Ctrl+Click)
            if (!e.CtrlKey && !_annotationService.SelectedAnnotationIds.Contains(annotationId))
            {
                // Regular click: clear selection and select only this annotation
                var newSelection = new HashSet<int> { annotationId };
                _annotationService.SelectMultiple(newSelection);
            }

            StateHasChanged();
        }

        private async Task UpdateResize(MouseEventArgs e)
        {
            if (!_isResizing || !_resizingAnnotationId.HasValue) return;

            var annotation = _annotations.FirstOrDefault(a => a.Id == _resizingAnnotationId.Value);
            if (annotation == null || annotation.Information.Count < 2) return;

            try
            {
                var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (coords != null && coords.Length == 2)
                {
                    var clampedPoint = ClampPointToImage(coords[0], coords[1]);
                    var newX = clampedPoint.X;
                    var newY = clampedPoint.Y;

                    // Update the appropriate corner based on the handle being dragged
                    switch (_resizeHandle)
                    {
                        case ResizeHandle.TopLeft:
                            annotation.Information[0] = new Point2f(newX, newY);
                            break;
                        case ResizeHandle.TopRight:
                            annotation.Information[0] = new Point2f(annotation.Information[0].X, newY);
                            annotation.Information[1] = new Point2f(newX, annotation.Information[1].Y);
                            break;
                        case ResizeHandle.BottomLeft:
                            annotation.Information[0] = new Point2f(newX, annotation.Information[0].Y);
                            annotation.Information[1] = new Point2f(annotation.Information[1].X, newY);
                            break;
                        case ResizeHandle.BottomRight:
                            annotation.Information[1] = new Point2f(newX, newY);
                            break;
                    }

                    // Normalize bbox points to ensure Point[0] is always top-left and Point[1] is always bottom-right
                    // This prevents weird behavior when dragging a corner past the opposite corner
                    var (minX, minY, maxX, maxY) = ImageCanvas.Helpers.GetBboxCoordinates(annotation.Information[0], annotation.Information[1]);
                    annotation.Information[0] = new Point2f((float)minX, (float)minY);
                    annotation.Information[1] = new Point2f((float)maxX, (float)maxY);

                    StateHasChanged();
                }
            }
            catch { }
        }

        private async Task UpdatePolygonPoint(MouseEventArgs e)
        {
            if (!_isMovingPoint || !_movingPointAnnotationId.HasValue) return;

            var annotation = _annotations.FirstOrDefault(a => a.Id == _movingPointAnnotationId.Value);
            if (annotation == null || _movingPointIndex < 0 || _movingPointIndex >= annotation.Information.Count) return;

            try
            {
                var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (coords != null && coords.Length == 2)
                {
                    annotation.Information[_movingPointIndex] = new Point2f(coords[0], coords[1]);
                    StateHasChanged();
                }
            }
            catch { }
        }

        private async Task EndEditing()
        {
            // Save bbox updates when resizing or moving completes
            if ((_isResizing || _isMovingAnnotation) && (_resizingAnnotationId.HasValue || _movingAnnotationId.HasValue))
            {
                var annotationId = _resizingAnnotationId ?? _movingAnnotationId;
                if (annotationId.HasValue)
                {
                    var annotation = _annotations.FirstOrDefault(a => a.Id == annotationId.Value);
                    if (annotation != null)
                    {
                        if (_projectWorkspaceService.CurrentProjectType?.Value == 1)
                        {
                            await OnBboxUpdated.InvokeAsync(annotation);
                        }
                        else if (_projectWorkspaceService.CurrentProjectType?.Value == 2)
                        {
                            await OnSegmentationUpdated.InvokeAsync(annotation);
                        }
                    }
                }
            }

            // Save segmentation updates when moving individual polygon points
            if (_isMovingPoint && _movingPointAnnotationId.HasValue)
            {
                var annotation = _annotations.FirstOrDefault(a => a.Id == _movingPointAnnotationId.Value);
                if (annotation != null && _projectWorkspaceService.CurrentProjectType?.Value == 2)
                {
                    await OnSegmentationUpdated.InvokeAsync(annotation);
                }
            }

            _isResizing = false;
            _resizingAnnotationId = null;
            _resizeHandle = ResizeHandle.None;
            _isMovingPoint = false;
            _movingPointAnnotationId = null;
            _movingPointIndex = -1;
            _isMovingAnnotation = false;
            _movingAnnotationId = null;
            _moveStartPoint = null;
            StateHasChanged();
        }

        private async Task StartMoveAnnotation(int annotationId, MouseEventArgs e)
        {
            if (_currentMode != LabelMode.Select) return;

            // Selection is handled by HandleAnnotationClick (via @onclick)
            // This method only handles the start of a move operation

            var annotation = _annotations.FirstOrDefault(a => a.Id == annotationId);
            if (annotation == null) return;

            try
            {
                var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (coords != null && coords.Length == 2)
                {
                    _isMovingAnnotation = true;
                    _movingAnnotationId = annotationId;
                    _moveStartPoint = new Point2f(coords[0], coords[1]);
                    StateHasChanged();
                }
            }
            catch { }
        }

        private async Task UpdateMoveAnnotation(MouseEventArgs e)
        {
            if (!_isMovingAnnotation || !_movingAnnotationId.HasValue || _moveStartPoint == null) return;

            var annotation = _annotations.FirstOrDefault(a => a.Id == _movingAnnotationId.Value);
            if (annotation == null) return;

            try
            {
                var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                    _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                if (coords != null && coords.Length == 2)
                {
                    var deltaX = coords[0] - _moveStartPoint.X;
                    var deltaY = coords[1] - _moveStartPoint.Y;

                    // For bbox annotations, we need to maintain the box shape while clamping
                    if (annotation.Information.Count == 2)
                    {
                        // Calculate what the new positions would be
                        var newX1 = annotation.Information[0].X + deltaX;
                        var newY1 = annotation.Information[0].Y + deltaY;
                        var newX2 = annotation.Information[1].X + deltaX;
                        var newY2 = annotation.Information[1].Y + deltaY;

                        // Get the normalized bbox bounds
                        var (minX, minY, maxX, maxY) = ImageCanvas.Helpers.GetBboxCoordinates(
                            new Point2f(newX1, newY1),
                            new Point2f(newX2, newY2)
                        );

                        // Clamp the bbox to stay within image bounds while maintaining its size
                        if (minX < 0)
                        {
                            var offset = 0 - minX;
                            newX1 += (float)offset;
                            newX2 += (float)offset;
                        }
                        else if (maxX > _canvasTransformService.ImageWidth)
                        {
                            var offset = _canvasTransformService.ImageWidth - maxX;
                            newX1 += (float)offset;
                            newX2 += (float)offset;
                        }

                        if (minY < 0)
                        {
                            var offset = 0 - minY;
                            newY1 += (float)offset;
                            newY2 += (float)offset;
                        }
                        else if (maxY > _canvasTransformService.ImageHeight)
                        {
                            var offset = _canvasTransformService.ImageHeight - maxY;
                            newY1 += (float)offset;
                            newY2 += (float)offset;
                        }

                        // Apply the clamped positions
                        annotation.Information[0] = new Point2f(newX1, newY1);
                        annotation.Information[1] = new Point2f(newX2, newY2);

                        // Update start point for next move
                        _moveStartPoint = new Point2f(coords[0], coords[1]);
                    }
                    else
                    {
                        // For polygons/segmentation, clamp each point individually
                        for (int i = 0; i < annotation.Information.Count; i++)
                        {
                            var newX = annotation.Information[i].X + deltaX;
                            var newY = annotation.Information[i].Y + deltaY;
                            annotation.Information[i] = ClampPointToImage(newX, newY);
                        }

                        // Update start point for next move
                        _moveStartPoint = new Point2f(coords[0], coords[1]);
                    }

                    StateHasChanged();
                }
            }
            catch { }
        }
    }
}
