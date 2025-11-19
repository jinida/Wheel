using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;
using WheelApp.Components;
using WheelApp.Pages.WheelDL.Coordinators;
using WheelApp.Pages.WheelDL.Models;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL
{
    public partial class Evaluate : ComponentBase, IDisposable
    {
        [Parameter] public string? Id { get; set; }
        [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;
        [Inject] private ProjectWorkspaceService _workspaceService { get; set; } = default!;
        [Inject] private ImageSelectionService _imageSelectionService { get; set; } = default!;
        [Inject] private ProjectWorkspaceCoordinator _workspaceCoordinator { get; set; } = default!;
        [Inject] private ProjectImageSelectionCoordinator _imageSelectionCoordinator { get; set; } = default!;
        [Inject] private ILogger<Evaluate> _logger { get; set; } = default!;

        // Phase 2 Helpers
        private KeyboardListenerHelper _keyboardCoordinator = default!;
        private UIEnhancementHelper _uiCoordinator = default!;
        private GridSortHelper _gridCoordinator = new();

        // Workspace data
        private ProjectWorkspaceDto? _workspace => _workspaceService.CurrentWorkspace;
        private List<ImageDto> AllImages => _workspaceService?.Images ?? new();
        private List<ImageDto> SortedImages => _gridCoordinator.GetSortedImages(AllImages).ToList();

        // Filtered images for Evaluate page (Validation and Test only)
        private List<ImageDto> FilteredImages => SortedImages.Where(img => img.RoleType?.Value == 1 || img.RoleType?.Value == 2).ToList();

        // Grid state properties
        private string? _sortColumn => _gridCoordinator.SortColumn;
        private bool _sortAscending => _gridCoordinator.SortAscending;

        // UI state
        private Toast _toastRef = null!;
        private ImageCanvas _imageCanvasRef = null!;
        private DotNetObjectReference<Evaluate>? _objRef;

        // Selection
        private ImageDto? _selectedImage => _imageSelectionCoordinator.CurrentImage;

        // Mock predictions
        private Dictionary<int, PredictionDto> _predictions = new();
        private bool _isLoadingPredictions = false;

        protected override async Task OnInitializedAsync()
        {
            // Initialize helpers early
            _uiCoordinator = new UIEnhancementHelper(_jsRuntime);
            _keyboardCoordinator = new KeyboardListenerHelper(_jsRuntime);

            if (int.TryParse(Id, out int projectId))
            {
                var result = await _workspaceCoordinator.LoadWorkspaceAsync(1);
                _workspaceService.SetLabelability(false);
                _workspaceService.NotifyDatasetChanged();

                if (!result.IsSuccess)
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to load evaluation", "error");
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Subscribe to workspace loaded event
                _workspaceService.OnWorkspaceLoaded += HandleWorkspaceLoaded;
                _workspaceService.SetLabelability(false);
                // Initialize keyboard listeners (only navigation shortcuts)
                _objRef = DotNetObjectReference.Create(this);
                await _keyboardCoordinator.InitializeKeyboardListenersAsync(this, "evaluate-grid-container");

                // Initialize UI enhancements with correct grid container ID
                await _uiCoordinator.InitializeUIEnhancementsAsync("evaluate-grid-container");

                StateHasChanged();
            }

            if (_selectedImage != null)
            {
                await _uiCoordinator.ScrollToImageAsync(_selectedImage.Id, "evaluate-grid-container");
            }
        }

        public void Dispose()
        {
            // Unsubscribe from events
            _workspaceService.OnWorkspaceLoaded -= HandleWorkspaceLoaded;

            _workspaceService.SetLabelability(false);
            _workspaceService.SetCurrentProject(null);
            _workspaceService.UpdateWorkspace(null);
            _workspaceService.NotifyDatasetChanged();
            _imageSelectionCoordinator.ClearAll();
            _gridCoordinator.ResetSort();

            // Cleanup keyboard listeners
            try
            {
                _ = _keyboardCoordinator?.CleanupKeyboardListenersAsync();
            }
            catch { }

            _objRef?.Dispose();
        }

        // Sorting
        private void ToggleSort(string columnName)
        {
            _gridCoordinator.SetSortColumn(columnName);
            StateHasChanged();
        }

        // Selection handlers
        private void HandleRowClickFromTable((ImageDto Image, MouseEventArgs Event) data)
        {
            HandleRowClick(data.Image, data.Event);
        }

        private void HandleRowClick(ImageDto image, MouseEventArgs e)
        {
            // Simple single selection for Evaluate page
            _imageSelectionService.SelectImage(image);
            StateHasChanged();
        }

        private void ChangeSelection(int offset)
        {
            if (!FilteredImages.Any()) return;

            var currentIdx = _selectedImage != null
                ? FilteredImages.FindIndex(i => i.Id == _selectedImage.Id)
                : 0;

            var newIdx = Math.Clamp(currentIdx + offset, 0, FilteredImages.Count - 1);
            var newImage = FilteredImages[newIdx];

            _imageSelectionService.SelectImage(newImage);
            StateHasChanged();
        }

        // Navigation
        private void NavigateBack()
        {
            _navigationManager.NavigateTo("/wheeldl/evaluates");
        }

        // Keyboard handlers - ONLY navigation, NO labeling shortcuts
        [JSInvokable]
        public async Task OnKeyPressed(string key)
        {
            // Only simple navigation, no multi-selection
            switch (key)
            {
                case "ArrowUp": ChangeSelection(-1); break;
                case "ArrowDown": ChangeSelection(1); break;
                case "ArrowLeft": ChangeSelection(-5); break;
                case "ArrowRight": ChangeSelection(5); break;
            }

            await Task.CompletedTask;
        }

        [JSInvokable]
        public async Task OnKeyPressedWithModifiers(Dictionary<string, object> keyInfo)
        {
            // Extract the key code from the dictionary
            var code = keyInfo.ContainsKey("code") ? keyInfo["code"].ToString() : "";

            // Only simple navigation, no multi-selection
            switch (code)
            {
                case "ArrowUp": ChangeSelection(-1); break;
                case "ArrowDown": ChangeSelection(1); break;
                case "ArrowLeft": ChangeSelection(-5); break;
                case "ArrowRight": ChangeSelection(5); break;
            }

            await Task.CompletedTask;
        }

        [JSInvokable]
        public async Task HandleGlobalCopyPaste(string action)
        {
            // Copy/paste disabled for evaluation mode
            await Task.CompletedTask;
        }

        private async void HandleWorkspaceLoaded()
        {
            _isLoadingPredictions = true;
            await InvokeAsync(StateHasChanged);

            // Generate mock predictions asynchronously
            await Task.Run(() => GenerateMockPredictions());

            _isLoadingPredictions = false;
            await InvokeAsync(StateHasChanged);
        }

        private Task HandleImageChanged(int dummy) => Task.CompletedTask;

        /// <summary>
        /// Gets prediction for the currently selected image
        /// </summary>
        private PredictionDto? GetPredictionForSelectedImage()
        {
            if (_selectedImage == null) return null;
            return _predictions.ContainsKey(_selectedImage.Id) ? _predictions[_selectedImage.Id] : null;
        }

        /// <summary>
        /// Generate mock prediction data based on project task type
        /// </summary>
        private void GenerateMockPredictions()
        {
            _predictions.Clear();

            if (_workspace == null || AllImages == null) return;

            var projectType = _workspace.ProjectType;
            var classes = _workspace.ProjectClasses;

            if (classes == null || !classes.Any()) return;

            foreach (var image in AllImages)
            {
                var prediction = new PredictionDto
                {
                    ImageId = image.Id,
                    Annotations = new List<PredictionAnnotationDto>()
                };

                switch (projectType)
                {
                    case 0: // Classification
                        prediction.Annotations.Add(GenerateClassificationPrediction(classes, image.Id));
                        break;

                    case 1: // Object Detection
                        var actualBBoxAnnotations = image.Annotation?.Where(a => a.classDto != null).ToList();
                        if (actualBBoxAnnotations != null && actualBBoxAnnotations.Any())
                        {
                            var bboxCount = Math.Min(actualBBoxAnnotations.Count, 3);
                            for (int i = 0; i < bboxCount; i++)
                            {
                                var detectionPrediction = GenerateDetectionPrediction(classes, image.Id, i, image);
                                if (detectionPrediction != null)
                                {
                                    prediction.Annotations.Add(detectionPrediction);
                                }
                            }
                        }
                        break;

                    case 2: // Segmentation
                        // Only generate predictions if actual annotations exist
                        var actualPolyAnnotations = image.Annotation?.Where(a => a.classDto != null).ToList();
                        if (actualPolyAnnotations != null && actualPolyAnnotations.Any())
                        {
                            var polyCount = Math.Min(actualPolyAnnotations.Count, 3);
                            for (int i = 0; i < polyCount; i++)
                            {
                                var segmentationPrediction = GenerateSegmentationPrediction(classes, image.Id, i, image);
                                if (segmentationPrediction != null)
                                {
                                    prediction.Annotations.Add(segmentationPrediction);
                                }
                            }
                        }
                        break;

                    case 3: // Anomaly Detection
                        prediction.Annotations.Add(GenerateAnomalyPrediction(classes, image.Id));
                        break;
                }

                _predictions[image.Id] = prediction;
            }
        }

        private PredictionAnnotationDto GenerateClassificationPrediction(List<ProjectClassDto> classes, int imageId)
        {
            var selectedClass = classes[imageId % classes.Count];
            return new PredictionAnnotationDto
            {
                ClassName = selectedClass.Name,
                ClassColor = selectedClass.Color,
                Confidence = 0.85 + (imageId % 15) * 0.01
            };
        }

        private PredictionAnnotationDto? GenerateDetectionPrediction(List<ProjectClassDto> classes, int imageId, int index, ImageDto image)
        {
            // Get actual annotations from the image
            var actualAnnotations = image.Annotation?.Where(a => a.classDto != null).ToList() ?? new List<AnnotationDto>();

            if (actualAnnotations.Any() && index < actualAnnotations.Count)
            {
                // Base prediction on actual annotation with slight variations
                var actualAnnotation = actualAnnotations[index];
                var bbox = actualAnnotation.Information;

                if (bbox != null && bbox.Count >= 2)
                {
                    // Add very small random offset to simulate prediction error (±2-5 pixels)
                    var random = new Random(imageId + index);
                    var offsetX = random.Next(-5, 6);
                    var offsetY = random.Next(-5, 6);
                    var scaleVariation = 0.95 + random.NextDouble() * 0.1; // 0.95 to 1.05

                    // bbox[0] = top-left, bbox[1] = bottom-right
                    var centerX = (bbox[0].X + bbox[1].X) / 2;
                    var centerY = (bbox[0].Y + bbox[1].Y) / 2;
                    var width = Math.Abs(bbox[1].X - bbox[0].X) * scaleVariation;
                    var height = Math.Abs(bbox[1].Y - bbox[0].Y) * scaleVariation;

                    var newX = centerX + offsetX - width / 2;
                    var newY = centerY + offsetY - height / 2;

                    return new PredictionAnnotationDto
                    {
                        ClassName = actualAnnotation.classDto!.Name,
                        ClassColor = actualAnnotation.classDto.Color,
                        BBox = new[]
                        {
                            new Point2f((float)newX, (float)newY),
                            new Point2f((float)(newX + width), (float)newY),
                            new Point2f((float)(newX + width), (float)(newY + height)),
                            new Point2f((float)newX, (float)(newY + height))
                        },
                        Confidence = 0.80 + random.NextDouble() * 0.19 // 0.80 to 0.99
                    };
                }
            }

            // No fallback - only generate if actual annotation exists
            return null;
        }

        private PredictionAnnotationDto? GenerateSegmentationPrediction(List<ProjectClassDto> classes, int imageId, int index, ImageDto image)
        {
            // Get actual annotations from the image
            var actualAnnotations = image.Annotation?.Where(a => a.classDto != null).ToList() ?? new List<AnnotationDto>();

            if (actualAnnotations.Any() && index < actualAnnotations.Count)
            {
                // Base prediction on actual annotation with slight variations
                var actualAnnotation = actualAnnotations[index];
                var polygon = actualAnnotation.Information;

                if (polygon != null && polygon.Count >= 3)
                {
                    // Add very small random offset to each point to simulate prediction error
                    var random = new Random(imageId + index);
                    var offsetScale = 0.98 + random.NextDouble() * 0.04; // 0.98 to 1.02

                    // Calculate center point
                    var polyCenterX = polygon.Average(p => p.X);
                    var polyCenterY = polygon.Average(p => p.Y);

                    // Transform each point with slight variation
                    var predictedPoints = polygon.Select(p =>
                    {
                        var offsetX = random.Next(-3, 4);
                        var offsetY = random.Next(-3, 4);

                        // Scale from center and add offset
                        var dx = (p.X - polyCenterX) * offsetScale + offsetX;
                        var dy = (p.Y - polyCenterY) * offsetScale + offsetY;

                        return new Point2f((float)(polyCenterX + dx), (float)(polyCenterY + dy));
                    }).ToArray();

                    return new PredictionAnnotationDto
                    {
                        ClassName = actualAnnotation.classDto!.Name,
                        ClassColor = actualAnnotation.classDto.Color,
                        Polygon = predictedPoints,
                        Confidence = 0.80 + random.NextDouble() * 0.19 // 0.80 to 0.99
                    };
                }
            }

            // No fallback - only generate if actual annotation exists
            return null;
        }

        private PredictionAnnotationDto GenerateAnomalyPrediction(List<ProjectClassDto> classes, int imageId)
        {
            // For anomaly detection, alternate between Normal and Abnormal
            var isNormal = imageId % 3 != 0;
            var selectedClass = classes.FirstOrDefault(c => c.Name.Contains(isNormal ? "Normal" : "Abnormal"))
                                ?? classes[imageId % classes.Count];

            return new PredictionAnnotationDto
            {
                ClassName = selectedClass.Name,
                ClassColor = selectedClass.Color,
                Confidence = isNormal ? 0.95 : 0.75 + (imageId % 20) * 0.01
            };
        }
    }
}
