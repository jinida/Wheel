using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;
using WheelApp.Components;
using WheelApp.Pages.WheelDL.Coordinators;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL
{
    public class KeyInfo
    {
        public string Code { get; set; } = "";
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
    }

    public partial class Project : ComponentBase, IDisposable
    {
        [Parameter] public string? Id { get; set; }
        [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;
        [Inject] private ProjectWorkspaceService _workspaceService { get; set; } = default!;
        [Inject] private DrawingToolService _drawingToolService { get; set; } = default!;
        [Inject] private AnnotationService _annotationService { get; set; } = default!;
        [Inject] private ClassManagementService _classManagementService { get; set; } = default!;
        [Inject] private ProjectWorkspaceCoordinator _workspaceCoordinator { get; set; } = default!;
        [Inject] private ProjectClassManagementCoordinator _classManagementCoordinator { get; set; } = default!;
        [Inject] private ProjectAnnotationCoordinator _annotationCoordinator { get; set; } = default!;
        [Inject] private ProjectRoleCoordinator _roleCoordinator { get; set; } = default!;
        [Inject] private ProjectFileCoordinator _fileCoordinator { get; set; } = default!;
        [Inject] private ProjectImageSelectionCoordinator _imageSelectionCoordinator { get; set; } = default!;

        // Phase 2 Helpers (directly instantiated - no heavy dependencies)
        private KeyboardListenerHelper _keyboardCoordinator = default!;
        private UIEnhancementHelper _uiCoordinator = default!;
        private GridSortHelper _gridCoordinator = new();

        // Prevent infinite loop when loading workspace
        private bool _isLoadingWorkspace = false;

        // Workspace data
        private ProjectWorkspaceDto? _workspace => _workspaceService.CurrentWorkspace;
        private List<ImageDto> AllImages => _workspaceService?.Images ?? new();
        private List<ImageDto> SortedImages => _gridCoordinator.GetSortedImages(AllImages).ToList();

        // Grid state properties (exposed for Razor binding)
        private string? _sortColumn => _gridCoordinator.SortColumn;
        private bool _sortAscending => _gridCoordinator.SortAscending;
        // UI state
        private Toast _toastRef = null!;
        private ImageCanvas _imageCanvasRef = null!;
        private DotNetObjectReference<Project>? _objRef;

        // Selection - All delegated to services (no local state)
        private ImageDto? _selectedImage => _imageSelectionCoordinator.CurrentImage;
        private ImageDto? _previousSelectedImage => _imageSelectionCoordinator.PreviousImage;
        private HashSet<int> _selectedImageIds => _imageSelectionCoordinator.SelectedImageIds;
        private HashSet<int> _animatingRows => _uiCoordinator?.AnimatingRows ?? new HashSet<int>();
        // Grid state - Delegated to UI coordinator
        private bool _isBulkProcessing
        {
            get => _uiCoordinator?.IsBulkProcessing ?? false;
            set { if (_uiCoordinator != null) _uiCoordinator.IsBulkProcessing = value; }
        }

        // Direct service references for updating state
        [Inject] private ImageSelectionService _imageSelectionService { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            // Initialize helpers early to avoid null reference during rendering
            _uiCoordinator = new UIEnhancementHelper(_jsRuntime);
            _keyboardCoordinator = new KeyboardListenerHelper(_jsRuntime);

            if (int.TryParse(Id, out int projectId))
            {
                var result = await _workspaceCoordinator.LoadWorkspaceAsync(projectId);
                if (!result.IsSuccess)
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to load project", "error");
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Subscribe to service events directly
                _drawingToolService.OnClassSelected += HandleClassSelection;
                _annotationService.OnSplitRoleSelected += HandleRoleSelection;
                _annotationService.OnAnnotationSelected += HandleAnnotationSelected;
                _annotationService.OnMultipleAnnotationsSelected += HandleMultipleAnnotationsSelected;
                _workspaceService.OnWorkspaceLoaded += HandleWorkspaceLoaded;
                _classManagementService.OnClassesChanged += HandleClassesChanged;
                _annotationService.OnAnnotationDeleted += HandleAnnotationRemoved;
                _annotationService.OnImportPreviousLabels += HandleImportPreviousLabelsFromSidebar;
                _annotationService.OnImageAnnotationsUpdated += HandleImageAnnotationsUpdated;

                // Initialize keyboard listeners
                _objRef = DotNetObjectReference.Create(this);
                await _keyboardCoordinator.InitializeKeyboardListenersAsync(this);

                // Initialize UI enhancements
                await _uiCoordinator.InitializeUIEnhancementsAsync();

                StateHasChanged();
            }

            if (_selectedImage != null)
            {
                await _uiCoordinator.ScrollToImageAsync(_selectedImage.Id);
            }
        }

        public void Dispose()
        {
            // Unsubscribe from events
            _drawingToolService.OnClassSelected -= HandleClassSelection;
            _annotationService.OnSplitRoleSelected -= HandleRoleSelection;
            _annotationService.OnAnnotationSelected -= HandleAnnotationSelected;
            _annotationService.OnMultipleAnnotationsSelected -= HandleMultipleAnnotationsSelected;
            _workspaceService.OnWorkspaceLoaded -= HandleWorkspaceLoaded;
            _classManagementService.OnClassesChanged -= HandleClassesChanged;
            _annotationService.OnAnnotationDeleted -= HandleAnnotationRemoved;
            _annotationService.OnImportPreviousLabels -= HandleImportPreviousLabelsFromSidebar;
            _annotationService.OnImageAnnotationsUpdated -= HandleImageAnnotationsUpdated;

            _workspaceService.SetLabelability(false);
            _workspaceService.SetCurrentProject(null);
            _workspaceService.UpdateWorkspace(null); // Clear workspace data
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
            // ToggleImageSelection already updates selected image, anchor, and previous in the service
            _imageSelectionCoordinator.ToggleImageSelection(image, e.CtrlKey, e.ShiftKey, SortedImages);

            // Sync with image selection service
            _imageSelectionService.SelectImage(image);
            StateHasChanged();
        }

        private void ChangeSelection(int offset)
        {
            _imageSelectionCoordinator.NavigateImage(offset, SortedImages, false);
            if (_selectedImage != null)
                _imageSelectionService.SelectImage(_selectedImage);
            StateHasChanged();
        }

        // Keyboard handlers
        [JSInvokable]
        public Task OnKeyPressed(string key) => OnKeyPressedWithModifiers(new KeyInfo { Code = key });

        [JSInvokable]
        public async Task HandleGlobalCopyPaste(string action)
        {
            if (_imageCanvasRef != null)
            {
                await _imageCanvasRef.HandleGlobalCopyPaste(action);
            }
        }

        [JSInvokable]
        public async Task OnKeyPressedWithModifiers(KeyInfo keyInfo)
        {
            if (keyInfo.Ctrl && (keyInfo.Code == "KeyC" || keyInfo.Code == "KeyV"))
                return;

            if (keyInfo.Ctrl && keyInfo.Code == "KeyA")
            {
                _imageSelectionCoordinator.SelectAllImages(SortedImages);
                _imageSelectionService.SetAnchor(SortedImages.FirstOrDefault()?.Id);
                StateHasChanged();
            }
            else if (keyInfo.Code == "Escape")
            {
                // ClearSelection already clears all state in the service
                _imageSelectionCoordinator.ClearSelection();
                StateHasChanged();
            }
            else if (keyInfo.Code == "Space" && _selectedImage != null)
            {
                // ToggleImageSelection already sets anchor in the service
                _imageSelectionCoordinator.ToggleImageSelection(_selectedImage, true, false, SortedImages);
                StateHasChanged();
            }
            else if (keyInfo.Shift && (keyInfo.Code == "ArrowUp" || keyInfo.Code == "ArrowDown"))
            {
                HandleShiftArrowNavigation(keyInfo.Code == "ArrowUp" ? -1 : 1);
            }
            else if (!keyInfo.Shift)
            {
                switch (keyInfo.Code)
                {
                    case "ArrowUp": ChangeSelection(-1); break;
                    case "ArrowDown": ChangeSelection(1); break;
                    case "ArrowLeft": ChangeSelection(-5); break;
                    case "ArrowRight": ChangeSelection(5); break;
                    case var digit when digit.StartsWith("Digit") && (_workspace?.ProjectType == 0 || _workspace?.ProjectType == 3):
                        await HandleDigitKey(keyInfo.Code);
                        break;
                    case "Backquote":
                        await ClearAnnotationsAndRolesForSelectedImages();
                        break;
                    case "KeyQ":
                        await SetRole(0); break;
                    case "KeyW":
                        await SetRole(1); break;
                    case "KeyE":
                        await SetRole(2); break;
                    case "KeyR":
                        await ImportPreviousLabelsForSelectedImages();
                        break;
                    case "KeyZ":
                        if (_workspace?.ProjectType == 0 || _workspace?.ProjectType == 3)
                            break;
                        _drawingToolService.SetMode(LabelMode.Select); break;
                    case "KeyX":
                        if (_workspace?.ProjectType == 0 || _workspace?.ProjectType == 3) break;
                        _drawingToolService.SetMode(LabelMode.Move); break;
                    case "KeyC":
                        if (_workspace?.ProjectType == 0 || _workspace?.ProjectType == 3) break;
                        var classCountKeyC = _workspaceService.ProjectClasses?.Count ?? 0;
                        if (classCountKeyC == 0)
                        {
                            _toastRef?.Show("No Classes", "Please create at least one class before using annotation tools.", "warning", 3000);
                            break;
                        }
                        _drawingToolService.SetMode(_workspace?.ProjectType == 2 ? LabelMode.Segmentation : LabelMode.BoundingBox);
                        break;
                }
            }
        }

        private async Task HandleDigitKey(string keyCode)
        {
            var digitIndex = keyCode switch
            {
                "Digit1" => 0, "Digit2" => 1, "Digit3" => 2, "Digit4" => 3, "Digit5" => 4,
                "Digit6" => 5, "Digit7" => 6, "Digit8" => 7, "Digit9" => 8, "Digit0" => 9,
                _ => -1
            };

            if (digitIndex >= 0 && _workspace != null)
            {
                var orderedClasses = _workspace.ProjectClasses.OrderBy(c => c.Id).ToList();
                if (digitIndex < orderedClasses.Count)
                {
                    await SetLabel(orderedClasses[digitIndex]);
                }
            }
        }

        private void HandleShiftArrowNavigation(int offset)
        {
            // NavigateImage already updates selected image in the service
            _imageSelectionCoordinator.NavigateImage(offset, SortedImages, true);
            if (_selectedImage != null)
                _imageSelectionService.SelectImage(_selectedImage);
            StateHasChanged();
        }

        // File trigger handlers
        private async Task TriggerImportLabel() => await _fileCoordinator.TriggerFileDialogAsync("import-label-input");
        private async Task TriggerAddImages() => await _fileCoordinator.TriggerFileDialogAsync("add-images-input");

        // Event handlers
        private async void HandleClassSelection(ProjectClassDto projectClass)
        {
            await SetLabel(projectClass);
        }

        private async void HandleRoleSelection(int roleType)
        {
            await SetRole(roleType);
        }

        private async void HandleRandomSplit()
        {
            if (!int.TryParse(Id, out int projectId)) return;

            _isBulkProcessing = true;
            StateHasChanged();

            try
            {
                var result = await _roleCoordinator.PerformRandomSplitAsync(projectId);
                if (result.IsSuccess)
                {
                    _toastRef?.Show("Success", result.Value?.Message ?? "Random split completed", "success");
                }
                else
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to perform random split", "error");
                }
            }
            finally
            {
                _isBulkProcessing = false;
                StateHasChanged();
            }
        }

        private async void HandleWorkspaceLoaded()
        {
            // Called when workspace is loaded/reloaded
            // Just update UI state, no reload needed (workspace is already loaded)
            await InvokeAsync(StateHasChanged);
        }

        private async void HandleInitializeAll()
        {
            // This should only be called after InitializeProjectCommand completes
            // to reload workspace and remove all annotations/roles from UI
            if (_isLoadingWorkspace)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            if (int.TryParse(Id, out int projectId))
            {
                _isBulkProcessing = true;
                _isLoadingWorkspace = true;
                StateHasChanged();

                try
                {
                    // Reload workspace to refresh UI (removes deleted annotations from canvas and sidebar)
                    await _workspaceCoordinator.LoadWorkspaceAsync(projectId);
                }
                finally
                {
                    _isBulkProcessing = false;
                    _isLoadingWorkspace = false;
                    StateHasChanged();
                }
            }
        }

        private async void HandleClassesChanged()
        {
            try
            {
                await _classManagementCoordinator.HandleClassesChangedAsync();

                if (_workspaceService.ProjectClasses?.Count == 0)
                {
                    _drawingToolService.SetMode(LabelMode.Move);
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                // Log exception and show error to user
                _toastRef?.Show("Error", "Failed to update classes", "error");
            }
        }

        private async Task HandleImportLabel(InputFileChangeEventArgs e)
        {
            if (!int.TryParse(Id, out int projectId)) return;

            _isBulkProcessing = true;
            StateHasChanged();

            try
            {
                var result = await _fileCoordinator.ImportAnnotationsAsync(e, projectId);
                if (result.Success)
                {
                    _toastRef?.Show("Import Successful", result.Message, result.FailedCount > 0 ? "warning" : "success", 4000);
                }
                else
                {
                    _toastRef?.Show("Import Error", result.Message, "error", 4000);
                }
            }
            finally
            {
                _isBulkProcessing = false;
                StateHasChanged();
            }
        }

        private async Task HandleExportLabel()
        {
            if (!int.TryParse(Id, out int projectId)) return;

            var result = await _fileCoordinator.ExportAnnotationsAsync(projectId, _workspace?.ProjectName);
            if (result.Success)
            {
                _toastRef?.Show("Export Successful", result.Message, "success", 3000);
            }
            else
            {
                _toastRef?.Show("Export Error", result.Message, "error", 4000);
            }
        }

        private async Task HandleAddImages(InputFileChangeEventArgs e)
        {
            if (!int.TryParse(Id, out int projectId) || _workspace == null) return;

            var result = await _fileCoordinator.UploadImagesAsync(e, projectId, _workspace.DatasetId);
            if (result.Success)
            {
                _toastRef?.Show("Images Added", result.Message, result.SkippedCount > 0 ? "warning" : "success", result.SkippedCount > 0 ? 5000 : 3000);
            }
            else
            {
                _toastRef?.Show("Upload Error", result.Message, "error", 4000);
            }
            StateHasChanged();
        }

        private async Task HandleDeleteImages()
        {
            if (!_selectedImageIds.Any())
            {
                _toastRef?.Show("No Selection", "Please select images to delete.", "warning", 3000);
                return;
            }

            if (!int.TryParse(Id, out int projectId)) return;

            var result = await _fileCoordinator.DeleteImagesAsync(_selectedImageIds.ToList(), projectId);
            if (result.Success)
            {
                // ClearSelection already clears all state (selected image, anchor, IDs)
                _imageSelectionCoordinator.ClearSelection();
                _toastRef?.Show("Images Deleted", result.Message, "success", 3000);
                StateHasChanged();
            }
            else
            {
                _toastRef?.Show("Delete Error", result.Message, "error", 4000);
            }
        }

        private async Task SetLabel(ProjectClassDto? projectClass)
        {
            if (_workspaceService.CurrentProjectType?.MultiLabelType == true) return;
            if (_workspace == null || !_selectedImageIds.Any()) return;

            // Show bulk processing overlay for 300+ images
            bool isBulkOperation = _selectedImageIds.Count >= 300;
            if (isBulkOperation)
            {
                _isBulkProcessing = true;
                StateHasChanged();
            }

            try
            {
                var result = await _annotationCoordinator.SetLabelAsync(_selectedImageIds.ToList(), projectClass);
                if (result.IsSuccess && result.Value != null)
                {
                    // Show toast if there were failures (Anomaly Detection business rules)
                    if (result.Value.FailedCount > 0)
                    {
                        var messageType = (result.Value.IsAnomalyDetection && result.Value.IsNormalClass) ? "info" : "warning";
                        _toastRef?.Show("Some Images Skipped", result.Value.Message, messageType, 4000);
                    }
                }
                else
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to set label.", "error", 4000);
                }
            }
            finally
            {
                if (isBulkOperation)
                {
                    _isBulkProcessing = false;
                }
                StateHasChanged();
            }
        }

        private async Task SetRole(int roleType)
        {
            if (_workspace == null || !_selectedImageIds.Any()) return;

            // Show bulk processing overlay for 300+ images
            bool isBulkOperation = _selectedImageIds.Count >= 300;
            if (isBulkOperation)
            {
                _isBulkProcessing = true;
                StateHasChanged();
            }

            try
            {
                var result = await _roleCoordinator.SetRoleAsync(_selectedImageIds.ToList(), roleType);
                if (result.IsSuccess && result.Value != null)
                {
                    // Show toast if there were failures or business rule adjustments (Anomaly Detection)
                    if (result.Value.NormalToValidationCount > 0)
                    {
                        _toastRef?.Show("Role Auto-Adjustment", result.Value.Message, "info", 5000);
                    }
                    else if (result.Value.FailedCount > 0)
                    {
                        _toastRef?.Show("Some Images Skipped", result.Value.Message, "warning", 4000);
                    }
                }
                else
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to set role.", "error", 4000);
                }
            }
            finally
            {
                if (isBulkOperation)
                {
                    _isBulkProcessing = false;
                }
                StateHasChanged();
            }
        }
        private async Task HandleBboxAdded(AnnotationDto bboxData)
        {
            if (_selectedImage == null) return;

            var result = await _annotationCoordinator.CreateBoundingBoxAsync(bboxData, _selectedImage.Id);
            if (!result.IsSuccess)
            {
                _toastRef?.Show("Error", result.Error ?? "Failed to save annotation.", "error", 4000);
            }
            StateHasChanged();
        }
        private async Task HandleBboxUpdated(AnnotationDto bboxUpdateData)
        {
            if (_selectedImage == null) return;

            var result = await _annotationCoordinator.UpdateBoundingBoxAsync(bboxUpdateData);
            if (!result.IsSuccess)
            {
                _toastRef?.Show("Error", result.Error ?? "Failed to update annotation.", "error", 4000);
            }
            StateHasChanged();
        }
        private async Task HandleSegmentationAdded(AnnotationDto segmentationData)
        {
            if (_selectedImage == null) return;

            var result = await _annotationCoordinator.CreateSegmentationAsync(segmentationData, _selectedImage.Id);
            if (!result.IsSuccess)
            {
                _toastRef?.Show("Error", result.Error ?? "Failed to save annotation.", "error", 4000);
            }
            StateHasChanged();
        }

        private async Task HandleSegmentationUpdated(AnnotationDto segmentationUpdateData)
        {
            if (_selectedImage == null) return;

            var result = await _annotationCoordinator.UpdateSegmentationAsync(segmentationUpdateData);
            if (!result.IsSuccess)
            {
                _toastRef?.Show("Error", result.Error ?? "Failed to update annotation.", "error", 4000);
            }
            StateHasChanged();
        }

        private void HandleAnnotationRemoved(int annotationId)
        {
            InvokeAsync(async () => await HandleAnnotationsRemovedBatch(new List<int> { annotationId }));
        }

        private void HandleImageAnnotationsUpdated(List<int> imageIds)
        {
            InvokeAsync(StateHasChanged);
        }

        private void HandleAnnotationSelected(int annotationId)
        {
            InvokeAsync(StateHasChanged);
        }

        private void HandleMultipleAnnotationsSelected(HashSet<int> annotationIds)
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task HandleAnnotationsRemovedBatch(List<int> annotationIds)
        {
            if (_selectedImage == null || annotationIds == null || !annotationIds.Any()) return;

            var result = await _annotationCoordinator.DeleteAnnotationsBatchAsync(annotationIds);
            if (!result.IsSuccess)
            {
                _toastRef?.Show("Error", result.Error ?? "Failed to delete annotations.", "error", 4000);
            }
            StateHasChanged();
        }

        private Task HandleImageChanged(int dummy) => Task.CompletedTask;

        private async Task ImportPreviousLabelsForSelectedImages()
        {
            if (_workspace == null || !_selectedImageIds.Any())
                return;

            // Show bulk processing overlay for 300+ images
            bool isBulkOperation = _selectedImageIds.Count >= 300;
            if (isBulkOperation)
            {
                _isBulkProcessing = true;
                StateHasChanged();
            }

            try
            {
                // Pass _previousSelectedImage directly (with full Annotation data from AllImages)
                var result = await _annotationCoordinator.ImportPreviousLabelsAsync(_selectedImageIds.ToList(), _previousSelectedImage);

                if (result.IsSuccess)
                {
                    _toastRef?.Show("Success", result.Value?.Message ?? "Labels imported successfully", "success", 3000);
                }
                else
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to import labels", "error", 4000);
                }
            }
            finally
            {
                if (isBulkOperation)
                {
                    _isBulkProcessing = false;
                }
                StateHasChanged();
            }
        }

        private async void HandleImportPreviousLabelsFromSidebar() => await ImportPreviousLabelsForSelectedImages();         

        private async Task ClearAnnotationsAndRolesForSelectedImages()
        {
            if (_workspace == null || !_selectedImageIds.Any())
            {
                _toastRef?.Show("Warning", "No images selected", "warning", 3000);
                return;
            }

            // Show bulk processing overlay for 300+ images
            bool isBulkOperation = _selectedImageIds.Count >= 300;
            if (isBulkOperation)
            {
                _isBulkProcessing = true;
                StateHasChanged();
            }

            try
            {
                var result = await _annotationCoordinator.ClearAllAnnotationsForImagesAsync(_selectedImageIds.ToList());

                if (result.IsSuccess)
                {
                    _toastRef?.Show("Success", result.Value?.Message ?? "Cleared annotations and roles", "success", 3000);
                    // Update service to point to refreshed workspace object
                    var updatedImage = AllImages.FirstOrDefault(img => img.Id == _selectedImage?.Id);
                    if (updatedImage != null)
                    {
                        _imageSelectionService.SelectImage(updatedImage);
                    }
                }
                else
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to clear data", "error", 4000);
                }
            }
            finally
            {
                if (isBulkOperation)
                {
                    _isBulkProcessing = false;
                }
                StateHasChanged();
            }
        }
    }
}
