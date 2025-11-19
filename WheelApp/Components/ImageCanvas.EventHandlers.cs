using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;
using WheelApp.Service;
using WheelApp.Services;

namespace WheelApp.Components
{
    /// <summary>
    /// ImageCanvas - Event Handlers (Mouse, Keyboard)
    /// Handles all mouse and keyboard events for canvas interaction
    /// </summary>
    public partial class ImageCanvas
    {
        private async Task HandleMouseDown(MouseEventArgs e)
        {
            if (!_imageLoaded) return;

            // Disable all labeling interactions in evaluation/view-only mode
            if (DisableLabeling && _currentMode != LabelMode.Move) return;

            // Don't start panning if we're editing annotations
            if (_isResizing || _isMovingPoint || _isMovingAnnotation) return;

            // Select mode: start drag selection when clicking on empty space
            if (_currentMode == LabelMode.Select)
            {
                // Start drag selection
                // Note: Annotation clicks have stopPropagation, so this only fires for empty space
                try
                {
                    var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                        _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                    if (coords != null && coords.Length == 2)
                    {
                        _isDraggingSelection = true;
                        _selectionStartPoint = new Point2f(coords[0], coords[1]);
                        _selectionEndPoint = new Point2f(coords[0], coords[1]);
                        StateHasChanged();
                    }
                }
                catch { }
                return;
            }
            else if (_currentMode == LabelMode.Move)
            {
                // Move mode: pan the canvas
                StartPan(e);
            }
            else if (_currentMode == LabelMode.BoundingBox || _currentMode == LabelMode.Segmentation)
            {
                // Drawing modes: start drawing new annotations
                await StartDrawing(e);
            }
        }

        private async Task HandleCanvasMouseMove(MouseEventArgs e)
        {
            if (!_imageLoaded) return;

            // Handle annotation editing operations (in Select mode)
            if (_isResizing)
            {
                await UpdateResize(e);
                return;
            }
            else if (_isMovingPoint)
            {
                await UpdatePolygonPoint(e);
                return;
            }
            else if (_isMovingAnnotation)
            {
                await UpdateMoveAnnotation(e);
                return;
            }
            else if (_isDraggingSelection)
            {
                // Update drag selection rectangle
                try
                {
                    var coords = await JSRuntime.InvokeAsync<int[]>("getImageCoordinates",
                        _imageElement, e.ClientX, e.ClientY, _canvasTransformService.Zoom, _canvasTransformService.PanX, _canvasTransformService.PanY);

                    if (coords != null && coords.Length == 2)
                    {
                        _selectionEndPoint = new Point2f(coords[0], coords[1]);
                        StateHasChanged();
                    }
                }
                catch { }
                return;
            }

            // Move mode: pan the canvas
            if (_currentMode == LabelMode.Move)
            {
                DoPan(e);
            }
            // Drawing modes: update current drawing preview
            else if (_currentDrawing != null)
            {
                await UpdateDrawing(e);
            }
        }

        private async Task HandleMouseUp(MouseEventArgs e)
        {
            if (!_imageLoaded) return;

            // End annotation editing operations (in Select mode)
            if (_isResizing || _isMovingPoint || _isMovingAnnotation)
            {
                EndEditing();
                return;
            }

            // End drag selection
            if (_isDraggingSelection)
            {
                // Select all annotations within the selection rectangle
                if (_selectionStartPoint != null && _selectionEndPoint != null)
                {
                    var (selX, selY, selWidth, selHeight) = ImageCanvas.Helpers.GetBboxBounds(_selectionStartPoint, _selectionEndPoint);
                    var selX2 = selX + selWidth;
                    var selY2 = selY + selHeight;

                    _annotationService.SelectedAnnotationIds.Clear();

                    foreach (var annotation in _annotations)
                    {
                        bool isWithinSelection = false;

                        if (_projectWorkspaceService.CurrentProjectType.Value == 1)
                        {
                            var (annX, annY, annWidth, annHeight) = ImageCanvas.Helpers.GetBboxBounds(annotation.Information[0], annotation.Information[1]);
                            var annX2 = annX + annWidth;
                            var annY2 = annY + annHeight;

                            // Check if bbox intersects with selection rectangle
                            isWithinSelection = !(annX2 < selX || annX > selX2 || annY2 < selY || annY > selY2);
                        }
                        else if (_projectWorkspaceService.CurrentProjectType.Value == 2)
                        {
                            // Check if any point of the polygon is within the selection rectangle
                            isWithinSelection = annotation.Information.Any(p =>
                                p.X >= selX && p.X <= selX2 && p.Y >= selY && p.Y <= selY2);
                        }

                        if (isWithinSelection)
                        {
                            _annotationService.SelectedAnnotationIds.Add(annotation.Id);
                        }
                    }
                }

                // Notify sidebar of all selected annotations
                _annotationService.SelectMultiple(new HashSet<int>(_annotationService.SelectedAnnotationIds));

                _isDraggingSelection = false;
                _selectionStartPoint = null;
                _selectionEndPoint = null;
                StateHasChanged();
                return;
            }

            // Move mode: end panning
            if (_currentMode == LabelMode.Move)
            {
                EndPan(e);
            }
            // Drawing modes: finish drawing
            else if (_currentMode == LabelMode.BoundingBox && _currentDrawing != null)
            {
                await FinishBoundingBox(e);
            }
        }

        private void HandleCanvasMouseLeave(MouseEventArgs e)
        {
            // End annotation editing operations if leaving (in Select mode)
            if (_isResizing || _isMovingPoint || _isMovingAnnotation)
            {
                EndEditing();
                return;
            }

            // Cancel drag selection if leaving
            if (_isDraggingSelection)
            {
                _isDraggingSelection = false;
                _selectionStartPoint = null;
                _selectionEndPoint = null;
                StateHasChanged();
                return;
            }

            // Move mode: end panning
            if (_currentMode == LabelMode.Move)
            {
                EndPan(e);
            }
            // Drawing modes: cancel or clear drawing preview
            else if (_currentDrawing != null && _currentMode == LabelMode.BoundingBox)
            {
                // Cancel current drawing
                _currentDrawing = null;
                _currentMousePosition = null;
                _isNearFirstPoint = false;
                StateHasChanged();
            }
            else if (_currentMode == LabelMode.Segmentation)
            {
                // Clear preview state when leaving during segmentation
                _currentMousePosition = null;
                _isNearFirstPoint = false;
                StateHasChanged();
            }
        }

        private async Task HandleDoubleClick(MouseEventArgs e)
        {
            // Double-click to finish polygon (only if not near first point)
            if (_currentMode == LabelMode.Segmentation && _currentDrawing != null && _projectWorkspaceService.CurrentProjectType.Value == 2 && !_isNearFirstPoint)
            {
                // Create a copy to avoid reference sharing
                var completedAnnotation = new AnnotationDto
                {
                    Id = _currentDrawing.Id,
                    classDto = _currentDrawing.classDto,
                    Information = _currentDrawing.Information.Select(p => new Point2f(p.X, p.Y)).ToList(),
                    CreatedAt = _currentDrawing.CreatedAt
                };
                _annotations.Add(completedAnnotation);

                // Save segmentation to database
                await OnSegmentationAdded.InvokeAsync(completedAnnotation);

                // Auto-select the newly created polygon
                _annotationService.SelectedAnnotationIds.Clear();
                _annotationService.SelectedAnnotationIds.Add(completedAnnotation.Id);
                _annotationService.SelectMultiple(new HashSet<int>(_annotationService.SelectedAnnotationIds));
                Logger.LogInformation("[ImageCanvas] New Polygon created with ID {Id}, auto-selected", completedAnnotation.Id);

                _currentDrawing = null;
                _currentMousePosition = null;
                _isNearFirstPoint = false;
                StateHasChanged();
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            // Disable keyboard shortcuts in evaluation/view-only mode
            if (DisableLabeling) return;

            if (e.Key == "Escape" && _currentDrawing != null)
            {
                _currentDrawing = null;
                _currentMousePosition = null;
                _isNearFirstPoint = false;
                StateHasChanged();
                return;
            }

            // Delete key to remove selected annotations (Delete, Backspace, or 'd' key)
            if ((e.Key == "Delete" || e.Key == "Backspace" || e.Key == "d" || e.Key == "D") && _annotationService.SelectedAnnotationIds.Count > 0)
            {
                var idsToDelete = _annotationService.SelectedAnnotationIds.ToList();

                // Remove from local list only - event handler will handle database deletion
                foreach (var deletedId in idsToDelete)
                {
                    _annotations.RemoveAll(a => a.Id == deletedId);
                }

                _annotationService.SelectedAnnotationIds.Clear();

                // Trigger deletion event ONCE for all annotations
                if (idsToDelete.Count > 1 && OnAnnotationsRemovedBatch.HasDelegate)
                {
                    await OnAnnotationsRemovedBatch.InvokeAsync(idsToDelete);
                }
                else if (idsToDelete.Count == 1)
                {
                    await OnAnnotationRemoved.InvokeAsync(idsToDelete[0]);
                }
                StateHasChanged();
                return;
            }

            // Numeric keys to change selected bbox class (1-9, 0)
            if (_annotationService.SelectedAnnotationIds.Count > 0)
            {
                int? classIndex = e.Key switch
                {
                    "1" => 0,
                    "2" => 1,
                    "3" => 2,
                    "4" => 3,
                    "5" => 4,
                    "6" => 5,
                    "7" => 6,
                    "8" => 7,
                    "9" => 8,
                    "0" => 9,
                    _ => null
                };

                if (classIndex.HasValue && classIndex.Value < _projectClasses.Count)
                {
                    var newClass = _projectClasses.ElementAtOrDefault(classIndex.Value);
                    if (newClass != null)
                    {
                        foreach (var annotationId in _annotationService.SelectedAnnotationIds.ToList())
                        {
                            var selectedAnnotation = _annotations.FirstOrDefault(a => a.Id == annotationId);
                            if (selectedAnnotation != null)
                            {
                                selectedAnnotation.classDto = newClass;
                                if (selectedAnnotation.Information.Count == 2)
                                {
                                    await OnBboxUpdated.InvokeAsync(selectedAnnotation);
                                }
                                else if (selectedAnnotation.Information.Count >= 3)
                                {
                                    await OnSegmentationUpdated.InvokeAsync(selectedAnnotation);
                                }
                            }
                        }

                        // Update selected class so new annotations will use this class
                        _drawingToolService.SetSelectedClassSilently(newClass);

                        StateHasChanged();
                    }
                }
            }
        }
    }
}
