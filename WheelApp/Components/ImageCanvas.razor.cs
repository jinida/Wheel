using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;
using WheelApp.Pages.WheelDL.Models;
using WheelApp.Service;
using WheelApp.Services;

namespace WheelApp.Components
{
    /// <summary>
    /// Resize handle enum for BBox resizing operations
    /// </summary>
    public enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// ImageCanvas - Main component for image annotation
    /// Handles image display, annotation rendering, and user interactions
    /// Split into multiple partial classes for better maintainability:
    /// - ImageCanvas.Transform.cs: Zoom, Pan, Image Adjustments
    /// - ImageCanvas.Drawing.cs: Drawing new annotations
    /// - ImageCanvas.Selection.cs: Selection and editing annotations
    /// - ImageCanvas.EventHandlers.cs: Mouse and keyboard events
    /// - ImageCanvas.Clipboard.cs: Copy, Paste, Import annotations
    /// - ImageCanvas.Helpers.cs: Utility methods
    /// </summary>
    public partial class ImageCanvas : ComponentBase, IDisposable
    {
        #region Parameters

        [Parameter] public ImageDto SelectedImage { get; set; }
        [Parameter] public EventCallback<AnnotationDto> OnBboxAdded { get; set; }
        [Parameter] public EventCallback<AnnotationDto> OnBboxUpdated { get; set; }
        [Parameter] public EventCallback<AnnotationDto> OnSegmentationAdded { get; set; }
        [Parameter] public EventCallback<AnnotationDto> OnSegmentationUpdated { get; set; }
        [Parameter] public EventCallback<int> OnAnnotationRemoved { get; set; }
        [Parameter] public EventCallback<List<int>> OnAnnotationsRemovedBatch { get; set; }
        [Parameter] public EventCallback<int> OnImageChanged { get; set; }
        [Parameter] public bool DisableLabeling { get; set; } = false;
        [Parameter] public PredictionDto? PredictionData { get; set; } = null;

        #endregion

        #region Injected Services

        [Inject] private ProjectWorkspaceService _projectWorkspaceService { get; set; } = default!;
        [Inject] private ImageSelectionService _imageSelectionService { get; set; } = default!;
        [Inject] private AnnotationService _annotationService { get; set; } = default!;
        [Inject] private DrawingToolService _drawingToolService { get; set; } = default!;
        [Inject] private ClassManagementService _classManagementService { get; set; } = default!;
        [Inject] private CanvasTransformService _canvasTransformService { get; set; } = default!;
        [Inject] private ILogger<ImageCanvas> Logger { get; set; } = default!;

        #endregion

        #region Private Fields

        private ElementReference _viewport;
        private ElementReference _imageElement;

        // Image loading state (local)
        private bool _imageLoaded = false;

        // Image adjustments (local)
        private double _brightness = 100;
        private double _gamma = 1.0;
        private double _contrast = 100;

        // Mouse coordinates display (local)
        private int? _mouseX = null;
        private int? _mouseY = null;
        private bool _showCoordinates = false;
        private double _lastMouseScreenX = 0;
        private double _lastMouseScreenY = 0;

        // Visibility toggles for evaluation mode (persist across image changes)
        private bool _showPredictions = true;
        private bool _showLabels = true;

        // Drawing state
        private LabelMode _currentMode => _drawingToolService.CurrentMode;
        private List<AnnotationDto> _annotations = new();
        private AnnotationDto? _currentDrawing = null;
        private List<ProjectClassDto> _projectClasses => _projectWorkspaceService.ProjectClasses;
        private Point2f? _currentMousePosition = null; // For preview line in segmentation
        private bool _isNearFirstPoint = false; // For closing polygon indicator
        private int? _previousImageId = null; // Track previous URL to detect changes

        private bool _isResizing = false;
        private int? _resizingAnnotationId = null;
        private ResizeHandle _resizeHandle = ResizeHandle.None;
        private bool _isMovingPoint = false;
        private int? _movingPointAnnotationId = null;
        private int _movingPointIndex = -1;
        private bool _isMovingAnnotation = false;
        private int? _movingAnnotationId = null;
        private Point2f? _moveStartPoint = null;
        private bool _isFinishingBbox = false;
        private bool _isDraggingSelection = false;
        private Point2f? _selectionStartPoint = null;
        private Point2f? _selectionEndPoint = null;
        private List<AnnotationDto>? _clipboard = null;
        private DotNetObjectReference<ImageCanvas>? _dotNetRef;
        private bool _resizeObserverInitialized = false;

        #endregion

        #region Lifecycle Methods

        protected override void OnInitialized()
        {
            // Subscribe to individual service events
            _drawingToolService.OnModeChanged += HandleToolSelected;
            _annotationService.OnAnnotationDeleted += HandleAnnotationDeleted;
            _annotationService.OnAllAnnotationsCleared += HandleAllAnnotationsCleared;
            _annotationService.OnAnnotationsByClassDeleted += HandleAnnotationsByClassDeleted;
            _drawingToolService.OnClassSelected += HandleClassSelected;
            _annotationService.OnAnnotationSelected += HandleAnnotationSelectedFromSidebar;
            _annotationService.OnMultipleAnnotationsSelected += HandleMultipleAnnotationsSelectedFromSidebar;
            _classManagementService.OnClassColorsUpdated += HandleClassColorsUpdated;
            _annotationService.OnAnnotationsClassChanged += HandleAnnotationsClassChanged;
            _annotationService.OnImageAnnotationsUpdated += HandleImageAnnotationsUpdated;

            // Initialize with the current selected class if one is already selected
            if (_drawingToolService.SelectedClass != null)
            {
                // SelectedClass is already set in the service, no need to duplicate
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
            }
        }

        [JSInvokable]
        public async Task OnViewportResized(double width, double height)
        {
            if (!_imageLoaded || _canvasTransformService.ImageWidth == 0 || _canvasTransformService.ImageHeight == 0)
            {
                // Image not loaded yet, just store viewport dimensions
                _canvasTransformService.SetViewportDimensions(width, height);
                return;
            }

            // Update viewport dimensions and recalculate base scale to recenter image
            _canvasTransformService.SetViewportDimensions(width, height);
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            // Cleanup ResizeObserver
            if (_resizeObserverInitialized)
            {
                try
                {
                    JSRuntime.InvokeVoidAsync("unobserveElementResize", _viewport);
                }
                catch
                {
                    // Ignore - component may already be disposed
                }
            }

            // Cleanup DotNetObjectReference
            _dotNetRef?.Dispose();

            // Unsubscribe from individual service events
            _drawingToolService.OnModeChanged -= HandleToolSelected;
            _annotationService.OnAnnotationDeleted -= HandleAnnotationDeleted;
            _annotationService.OnAllAnnotationsCleared -= HandleAllAnnotationsCleared;
            _annotationService.OnAnnotationsByClassDeleted -= HandleAnnotationsByClassDeleted;
            _drawingToolService.OnClassSelected -= HandleClassSelected;
            _annotationService.OnAnnotationSelected -= HandleAnnotationSelectedFromSidebar;
            _annotationService.OnMultipleAnnotationsSelected -= HandleMultipleAnnotationsSelectedFromSidebar;
            _classManagementService.OnClassColorsUpdated -= HandleClassColorsUpdated;
            _annotationService.OnAnnotationsClassChanged -= HandleAnnotationsClassChanged;
            _annotationService.OnImageAnnotationsUpdated -= HandleImageAnnotationsUpdated;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (SelectedImage == null) return;

            bool isNewImage = SelectedImage.Id != _previousImageId;

            // Always sync annotations with SelectedImage (handles workspace reload, class changes, etc.)
            bool annotationsChanged = SelectedImage.Annotation?.Count != _annotations.Count;

            // Reset state when image changes OR when annotations change (e.g., after ImportPreviousLabels)
            if (isNewImage)
            {
                _previousImageId = SelectedImage.Id;
                // Reset zoom and position for new image
                _canvasTransformService.ResetAll();
                _imageLoaded = false;
                _mouseX = null;
                _mouseY = null;

                // Load annotations from database
                _annotations.Clear();
                if (SelectedImage.Annotation != null && SelectedImage.Annotation.Any())
                {
                    _annotations.AddRange(SelectedImage.Annotation);
                    Logger.LogInformation("[ImageCanvas] Loaded {Count} annotations from database for image {ImageId}",
                        SelectedImage.Annotation.Count, SelectedImage.Id);
                }
                _currentDrawing = null;
                _annotationService.SelectedAnnotationIds.Clear();
            }
            else if (annotationsChanged)
            {
                // Same image, but annotations changed (e.g., ImportPreviousLabels, ClearAnnotations, or workspace reload)
                _annotations.Clear();
                if (SelectedImage.Annotation != null && SelectedImage.Annotation.Any())
                {
                    _annotations.AddRange(SelectedImage.Annotation);
                    Logger.LogInformation("[ImageCanvas] Updated {Count} annotations for same image {ImageId}",
                        SelectedImage.Annotation.Count, SelectedImage.Id);
                }
                _currentDrawing = null;
                _annotationService.SelectedAnnotationIds.Clear();

                // Trigger re-render to show new annotations (don't reset zoom/pan)
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleImageLoad()
        {
            try
            {
                // Get image dimensions
                var dimensions = await JSRuntime.InvokeAsync<int[]>("getImageDimensions", _imageElement);
                if (dimensions != null && dimensions.Length == 2)
                {
                    _canvasTransformService.SetImageDimensions(dimensions[0], dimensions[1]);
                    if (SelectedImage != null && !string.IsNullOrEmpty(SelectedImage.Url))
                    {
                        await OnImageChanged.InvokeAsync(0);
                    }

                    ClampAllAnnotationsToImage();

                    // Calculate base scale after image load
                    await CalculateBaseScale();

                    // Only set _imageLoaded after all calculations are complete
                    _imageLoaded = true;

                    // Initialize ResizeObserver after image is loaded
                    if (!_resizeObserverInitialized && _dotNetRef != null)
                    {
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("observeElementResize", _viewport, _dotNetRef, "OnViewportResized");
                            _resizeObserverInitialized = true;
                        }
                        catch
                        {
                            // Silently ignore - ResizeObserver setup failed
                        }
                    }
                }
            }
            catch
            {
                _imageLoaded = true; // Set to true even on error to avoid infinite waiting
            }

            StateHasChanged();
        }

        private async Task CalculateBaseScale()
        {
            if (_canvasTransformService.ImageWidth == 0 || _canvasTransformService.ImageHeight == 0) return;

            try
            {
                // Get viewport dimensions
                var viewportDimensions = await JSRuntime.InvokeAsync<double[]>("getElementDimensions", _viewport);
                if (viewportDimensions == null || viewportDimensions.Length < 2)
                {
                    return;
                }

                // SetViewportDimensions will automatically call CalculateBaseScale in the service
                _canvasTransformService.SetViewportDimensions(viewportDimensions[0], viewportDimensions[1]);

                Logger.LogInformation("[ImageCanvas] Calculated baseScale: {BaseScale}, zoom: {Zoom}, viewport: {VW}x{VH}, image: {IW}x{IH}, pan: ({PX}, {PY})",
                    _canvasTransformService.BaseScale, _canvasTransformService.Zoom, _canvasTransformService.ViewportWidth, _canvasTransformService.ViewportHeight, _canvasTransformService.ImageWidth, _canvasTransformService.ImageHeight, _canvasTransformService.PanX, _canvasTransformService.PanY);

                StateHasChanged();
            }
            catch
            {
                // If calculation fails, service will use default values
            }
        }

        #endregion

        #region Service Event Handlers

        private void HandleToolSelected(LabelMode tool)
        {
            // For Classification (0) and Anomaly Detection (3) projects, only allow Select and Move modes
            var projectType = _projectWorkspaceService.CurrentProjectType?.Value;
            if (projectType == 0 || projectType == 3)
            {
                // Only allow Select and Move tools
                if (tool != LabelMode.Select && tool != LabelMode.Move)
                {
                    return; // Ignore the tool change
                }
            }

            if (_currentDrawing != null)
            {
                _currentDrawing = null;
                _currentMousePosition = null;
                _isNearFirstPoint = false;
            }

            StateHasChanged();
        }

        private async void HandleAnnotationDeleted(int id)
        {
            _annotations.RemoveAll(a => a.Id == id);
            _annotationService.SelectedAnnotationIds.Remove(id);
            await OnAnnotationRemoved.InvokeAsync(id);
            StateHasChanged();
        }

        private async void HandleAllAnnotationsCleared()
        {
            var annotationIds = _annotations.Select(a => a.Id).ToList();
            _annotations.Clear();
            _currentDrawing = null;
            _annotationService.SelectedAnnotationIds.Clear();

            if (annotationIds.Count > 0 && OnAnnotationsRemovedBatch.HasDelegate)
            {
                await OnAnnotationsRemovedBatch.InvokeAsync(annotationIds);
            }

            StateHasChanged();
        }

        private void HandleAnnotationsByClassDeleted(ProjectClassDto projectClass)
        {
            // Note: Annotations are already deleted from database by DeleteProjectClassCommandHandler
            // This handler only needs to update the UI state

            var deletedIds = _annotations.Where(a => a.classDto.Id == projectClass.Id).Select(a => a.Id).ToList();

            _annotations.RemoveAll(a => a.classDto?.Id == projectClass.Id);

            foreach (var id in deletedIds)
            {
                _annotationService.SelectedAnnotationIds.Remove(id);
            }

            // Clear selected class if it was deleted
            if (_drawingToolService.SelectedClass != null && _drawingToolService.SelectedClass.Id == projectClass.Id)
            {
                if (_projectClasses.Count > 0)
                {
                    _drawingToolService.SetSelectedClassSilently(_projectClasses[0]);
                }
                else
                {
                    _drawingToolService.ClearSelectedClass();
                }
            }

            // DO NOT call OnAnnotationsRemovedBatch - annotations already deleted in command handler

            StateHasChanged();
        }

        private async void HandleClassSelected(ProjectClassDto selectedClass)
        {
            _drawingToolService.SetSelectedClassSilently(selectedClass);

            // Find the ProjectClassDto for this class name
            var projectClass = _projectClasses.FirstOrDefault(c => c.Id == selectedClass.Id);
            if (projectClass == null) return;

            // If there are selected annotations, change their class
            foreach (var annotationId in _annotationService.SelectedAnnotationIds.ToList())
            {
                var selectedAnnotation = _annotations.FirstOrDefault(a => a.Id == annotationId);
                if (selectedAnnotation != null)
                {
                    selectedAnnotation.classDto = projectClass;

                    if (_projectWorkspaceService.CurrentProjectType?.Value == 1)
                    {
                        await OnBboxUpdated.InvokeAsync(selectedAnnotation);
                    }
                    else if (_projectWorkspaceService.CurrentProjectType?.Value == 2)
                    {
                        await OnSegmentationUpdated.InvokeAsync(selectedAnnotation);
                    }
                }
            }

            StateHasChanged();
        }

        private void HandleAnnotationSelectedFromSidebar(int id)
        {
            // This is called when sidebar changes selection - just update local view, don't trigger events again
            _annotationService.SelectedAnnotationIds.Clear();
            _annotationService.SelectedAnnotationIds.Add(id);
            StateHasChanged();
        }

        private void HandleMultipleAnnotationsSelectedFromSidebar(HashSet<int> ids)
        {
            // This is called when sidebar changes selection - just update local view, don't trigger events again
            _annotationService.SelectedAnnotationIds.Clear();
            foreach (var id in ids)
            {
                _annotationService.SelectedAnnotationIds.Add(id);
            }
            StateHasChanged();
        }

        private void HandleClassColorsUpdated(List<ProjectClassDto>? classes)
        {
            StateHasChanged();
        }

        private void HandleAnnotationsClassChanged(List<int> annotationIds, int newClassId)
        {
            Logger.LogInformation("[ImageCanvas] HandleAnnotationsClassChanged - {Count} annotations to class {ClassId}",
                annotationIds.Count, newClassId);

            // Find the new class from workspace
            var newClass = _projectWorkspaceService.CurrentWorkspace?.ProjectClasses.FirstOrDefault(c => c.Id == newClassId);
            if (newClass == null)
            {
                Logger.LogWarning("[ImageCanvas] Class {ClassId} not found in workspace", newClassId);
                return;
            }

            // Update annotations in canvas
            foreach (var annotationId in annotationIds)
            {
                var annotation = _annotations.FirstOrDefault(a => a.Id == annotationId);
                if (annotation != null)
                {
                    annotation.classDto = newClass;
                    Logger.LogInformation("[ImageCanvas] Updated annotation {AnnotationId} to class {ClassName}",
                        annotationId, newClass.Name);
                }
            }

            StateHasChanged();
        }

        /// <summary>
        /// Handles notification that annotations were added to specific images
        /// Reloads annotations if current image was updated
        /// </summary>
        private void HandleImageAnnotationsUpdated(List<int> imageIds)
        {
            // Only reload if current image is in the list of updated images
            if (SelectedImage != null && imageIds.Contains(SelectedImage.Id))
            {
                Logger.LogInformation("[ImageCanvas] HandleImageAnnotationsUpdated - Before: _annotations={Count}, SelectedImage.Annotation={AnnotationCount}",
                    _annotations.Count, SelectedImage.Annotation?.Count ?? 0);

                // IMPORTANT: Get the latest image from workspace, not from SelectedImage parameter
                // After workspace reload (e.g., class creation), SelectedImage may reference an old object
                var workspace = _projectWorkspaceService.CurrentWorkspace;
                var updatedImage = workspace?.Images.FirstOrDefault(img => img.Id == SelectedImage.Id);

                _annotations.Clear();
                if (updatedImage?.Annotation != null && updatedImage.Annotation.Any())
                {
                    _annotations.AddRange(updatedImage.Annotation);
                    Logger.LogInformation("[ImageCanvas] Reloaded {Count} annotations from workspace for image {ImageId}",
                        updatedImage.Annotation.Count, SelectedImage.Id);

                    // Debug: Log each annotation's details to diagnose rendering issues
                    foreach (var annotation in _annotations)
                    {
                        Logger.LogInformation("[ImageCanvas] Annotation {Id}: classDto={ClassName} (Id={ClassId}), Information={InfoCount} points, ProjectType={ProjectType}",
                            annotation.Id,
                            annotation.classDto?.Name ?? "NULL",
                            annotation.classDto?.Id ?? -1,
                            annotation.Information?.Count ?? 0,
                            _projectWorkspaceService.CurrentProjectType?.Value ?? -1);
                    }
                }

                Logger.LogInformation("[ImageCanvas] HandleImageAnnotationsUpdated - After: _annotations={Count}", _annotations.Count);

                // Force UI update - use InvokeAsync to ensure we're on UI thread
                _ = InvokeAsync(() =>
                {
                    StateHasChanged();
                    Logger.LogInformation("[ImageCanvas] StateHasChanged called after annotation update");
                });
            }
        }

        #endregion
    }
}
