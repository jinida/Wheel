using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Trainings.Queries.GetActiveTrainings;
using WheelApp.Components;
using WheelApp.Pages.WheelDL.Coordinators;
using WheelApp.Services;
using WheelApp.State;

namespace WheelApp.Shared
{
    /// <summary>
    /// Right sidebar component for project class and annotation management
    /// Follows Clean Architecture by receiving data from parent component via StateService
    /// All data loading is done by parent component using GetProjectWorkspaceQuery
    /// </summary>
    public partial class RightSideBar : ComponentBase, IDisposable
    {
        [Parameter]
        public bool IsCollapsed { get; set; }
        [Inject]
        private ProjectWorkspaceService _workspaceService { get; set; } = default!;
        [Inject]
        private ImageSelectionService _imageSelectionService { get; set; } = default!;
        [Inject]
        private AnnotationService _annotationService { get; set; } = default!;
        [Inject]
        private ClassManagementService _classManagementService { get; set; } = default!;
        [Inject]
        private RightSideBarCoordinator _coordinator { get; set; } = default!;
        [Inject]
        private DrawingToolService _drawingToolService { get; set; } = default!;
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject]
        private IMediator _mediator { get; set; } = default!;
        [Inject]
        private ILogger<RightSideBar> _logger { get; set; } = default!;

        private List<TrainingDto> _activeTrainings = new();
        private System.Threading.Timer? _trainingPollingTimer;
        private bool _isLoadingTrainings = false;
        private HashSet<int> _expandedTrainingIds = new();

        private readonly ModalManager _modalManager = new();


        private List<ProjectClassDto> _classItems => _workspaceService.ProjectClasses;
        private List<AnnotationDto>? _annotations => _imageSelectionService.SelectedImage?.Annotation;

        private ClassModal? _classModalRef;
        private EditClassModal? _editClassModalRef;
        private Toast? _toastRef;

        private bool _isCreateClassModalVisible => _modalManager.IsVisible(ModalManager.ModalKeys.CreateClass);
        private bool _isEditClassModalVisible => _modalManager.IsVisible(ModalManager.ModalKeys.EditClass);

        private string _activeTab = "Process";
        private LabelMode _selectedTool => _drawingToolService?.CurrentMode ?? LabelMode.Move;

        private HashSet<int> _selectedAnnotationIds => _annotationService.SelectedAnnotationIds;
        private string? _openClassMenuId = null;
        private int? _openAnnotationMenuId = null;

        protected override void OnInitialized()
        {
            // Validate all required injected dependencies
            if (_workspaceService == null) throw new InvalidOperationException("ProjectWorkspaceService service not injected");
            if (_imageSelectionService == null) throw new InvalidOperationException("ImageSelectionService service not injected");
            if (_annotationService == null) throw new InvalidOperationException("AnnotationService service not injected");
            if (_classManagementService == null) throw new InvalidOperationException("ClassManagementService service not injected");
            if (_coordinator == null) throw new InvalidOperationException("RightSideBarCoordinator service not injected");
            if (_drawingToolService == null) throw new InvalidOperationException("DrawingToolService service not injected");
            if (JSRuntime == null) throw new InvalidOperationException("IJSRuntime service not injected");

            // Subscribe to service events directly
            _workspaceService.OnWorkspaceLoaded += HandleWorkspaceLoaded;
            _workspaceService.OnDatasetChanged += HandleStateChange;
            _annotationService.OnAnnotationSelected += HandleAnnotationSelectedFromCanvas;
            _annotationService.OnMultipleAnnotationsSelected += HandleMultipleAnnotationsSelectedFromCanvas;
            _annotationService.OnAnnotationDeleted += HandleAnnotationDeletedFromCanvas;
            _annotationService.OnAllAnnotationsCleared += HandleAllAnnotationsCleared;
            _annotationService.OnImageAnnotationsUpdated += HandleImageAnnotationsUpdated;
            _drawingToolService.OnClassSelected += HandleClassSelected;
            _drawingToolService.OnToolSelected += HandleToolSelected;
            _classManagementService.OnClassesChanged += HandleClassesChanged;
            _imageSelectionService.OnImageSelected += HandleImageSelected;

            // IMPORTANT: Initialize UI based on project type when page first loads
            UpdateUIBasedOnTaskType();

            // Polling disabled - uncomment to enable
            // _trainingPollingTimer = new System.Threading.Timer(async _ => await LoadActiveTrainings(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private async Task LoadActiveTrainings()
        {
            // Prevent concurrent execution
            if (_isLoadingTrainings) return;

            try
            {
                _isLoadingTrainings = true;

                var query = new GetActiveTrainingsQuery();
                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Value != null)
                {
                    var previousCount = _activeTrainings.Count;
                    _activeTrainings = result.Value;

                    // Remove completed trainings (they disappear from active list)
                    if (_activeTrainings.Count < previousCount)
                    {
                        await InvokeAsync(StateHasChanged);
                    }
                    else if (_activeTrainings.Any())
                    {
                        // Update UI if there are running trainings (to show progress updates)
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active trainings");
            }
            finally
            {
                _isLoadingTrainings = false;
            }
        }

        private string GetStatusClass(int status)
        {
            return status switch
            {
                0 => "status-pending",
                1 => "status-running",
                2 => "status-completed",
                3 => "status-failed",
                _ => ""
            };
        }

        private void HandleWorkspaceLoaded()
        {
            // When workspace is loaded, update UI to switch to Label tab if CanLabel is true
            UpdateUIBasedOnTaskType();
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when classes are updated from parent component
        /// </summary>
        private async void HandleClassesChanged()
        {
            // Update local class list from StateService
            await InvokeAsync(StateHasChanged);
        }

        private async void HandleImageSelected(ImageDto? image)
        {
            _annotationService.ClearSelection();
            _logger.LogInformation("[RightSideBar] Image changed, cleared annotation selection");
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Handles notification that annotations were added to specific images
        /// Refreshes annotations if current image was updated
        /// </summary>
        private async void HandleImageAnnotationsUpdated(List<int> imageIds)
        {
            // Only refresh if current image is in the list of updated ima
            await InvokeAsync(StateHasChanged);
        }


        private void HandleClassSelected(ProjectClassDto projectClass)
        {
            InvokeAsync(StateHasChanged);
        }

        private void HandleStateChange()
        {
            // Use ProjectWorkspaceService instead of deprecated SidebarStateService
            if (!_workspaceService.CanLabel)
            {
                _activeTab = "Process";
            }
            else
            {
                // Update UI based on task type (includes auto-tab switching for ObjectDetection/Segmentation)
                UpdateUIBasedOnTaskType();
            }

            InvokeAsync(StateHasChanged);
        }

        private void HandleAllAnnotationsCleared()
        {
            _annotations?.Clear();
            _selectedAnnotationIds.Clear();
            InvokeAsync(StateHasChanged);
        }

        private void HandleAnnotationDeletedFromCanvas(int id)
        {
            // Remove annotation from the sidebar list
            _annotations?.RemoveAll(a => a.Id == id);
            _selectedAnnotationIds.Remove(id);
            _logger.LogInformation("[RightSideBar] Removed annotation {AnnotationId} from sidebar list (keyboard delete)", id);
            InvokeAsync(StateHasChanged);
        }

        private void HandleAnnotationSelectedFromCanvas(int id)
        {
            // For now, single selection from canvas - clear and select one
            _selectedAnnotationIds.Clear();
            _selectedAnnotationIds.Add(id);
            _logger.LogInformation("[RightSideBar] Annotation {AnnotationId} selected from canvas", id);
            InvokeAsync(StateHasChanged);
        }

        private void HandleMultipleAnnotationsSelectedFromCanvas(HashSet<int> ids)
        {
            // Update sidebar selection to match canvas selection
            _logger.LogInformation("[RightSideBar] Multiple annotations selected from canvas: [{Ids}]", string.Join(", ", ids));
            InvokeAsync(StateHasChanged);
        }

        private void HandleToolSelected(LabelMode tool)
        {
            _drawingToolService.SetMode(tool);
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Updates UI based on current project type
        /// Sets default classes for specific project types
        /// Note: This is only for default/fallback UI, actual classes come from StateService
        /// </summary>
        private void UpdateUIBasedOnTaskType()
        {
            // Use ProjectWorkspaceService instead of deprecated SidebarStateService
            var currentProjectType = _workspaceService.CurrentProjectType;

            // Check if we have a valid project type
            if (currentProjectType == null)
            {
                return;
            }

            // Switch to Label tab for all project types when CanLabel is true
            if (_workspaceService.CanLabel)
            {
                _activeTab = "Label";
            }

            _drawingToolService.SetMode(LabelMode.Move);

            // Auto-select first class if available for Classification
            if (_classItems.Count > 0)
            {
                _drawingToolService.SelectClass(_classItems[0]);
            }
        }

        public void Dispose()
        {
            _trainingPollingTimer?.Dispose();

            _workspaceService.OnWorkspaceLoaded -= HandleWorkspaceLoaded;
            _workspaceService.OnDatasetChanged -= HandleStateChange;
            _annotationService.OnAnnotationSelected -= HandleAnnotationSelectedFromCanvas;
            _annotationService.OnMultipleAnnotationsSelected -= HandleMultipleAnnotationsSelectedFromCanvas;
            _annotationService.OnAnnotationDeleted -= HandleAnnotationDeletedFromCanvas;
            _annotationService.OnAllAnnotationsCleared -= HandleAllAnnotationsCleared;
            _annotationService.OnImageAnnotationsUpdated -= HandleImageAnnotationsUpdated;
            _drawingToolService.OnClassSelected -= HandleClassSelected;
            _drawingToolService.OnToolSelected -= HandleToolSelected;
            _classManagementService.OnClassesChanged -= HandleClassesChanged;
            _imageSelectionService.OnImageSelected -= HandleImageSelected;
        }

        private void HideAddClassModal() => _modalManager.Hide(ModalManager.ModalKeys.CreateClass);
        private async void HandleAddClassSubmit(ClassModal.Model model)
        {
            if (string.IsNullOrWhiteSpace(model.ClassName)) return;

            try
            {
                var result = await _coordinator.CreateClassAsync(model.ClassName, model.ClassColor);

                if (!result.IsSuccess)
                {
                    ParseAndShowValidationErrors(result?.Error, _classModalRef);
                    return;
                }

                HideAddClassModal();
                _classModalRef?.ClearForm();
                ShowToast("Success", $"Class '{model.ClassName}' created successfully.", "success");
                await InvokeAsync(StateHasChanged);
            }
            catch (Application.Common.Exceptions.ValidationException ex)
            {
                ParseAndShowValidationErrors(ex, _classModalRef);
            }
        }

        private void ParseAndShowValidationErrors(Application.Common.Exceptions.ValidationException ex, ClassModal? modalRef)
        {
            foreach (var error in ex.Errors)
            {
                var errorMessage = string.Join(" ", error.Value);
                if (error.Key == "Name")
                {
                    modalRef?.SetValidationError("Name", errorMessage);
                }
                else if (error.Key == "Color")
                {
                    modalRef?.SetValidationError("Color", errorMessage);
                }
            }
        }

        private void ParseAndShowValidationErrors(string error, ClassModal? modalRef)
        {
            if (error.Contains("name", StringComparison.OrdinalIgnoreCase) && error.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                modalRef?.SetValidationError("Name", error);
            }
            else if (error.Contains("color", StringComparison.OrdinalIgnoreCase) && error.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                modalRef?.SetValidationError("Color", error);
            }
            else
            {
                HideAddClassModal();
                _classModalRef?.ClearForm();
                ShowToast("Error", error, "error");
            }
        }

        private void ParseAndShowValidationErrors(string error, EditClassModal? modalRef)
        {
            if (error.Contains("name", StringComparison.OrdinalIgnoreCase) && error.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                modalRef?.SetValidationError("Name", error);
            }
            else if (error.Contains("color", StringComparison.OrdinalIgnoreCase) && error.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                modalRef?.SetValidationError("Color", error);
            }
            else
            {
                ShowToast("Error", error, "error");
            }
        }

        private void HandleClassItemClick(ProjectClassDto projectClass)
        {
            _drawingToolService.SelectClass(projectClass);
        }

        private async void ToggleClassMenu(string className)
        {
            if (_openClassMenuId == className)
            {
                _openClassMenuId = null;
                await JSRuntime.InvokeVoidAsync("cleanupDropdownListener");
            }
            else
            {
                _openClassMenuId = className;
            }
            await InvokeAsync(StateHasChanged);

            // Position the dropdown after render
            if (_openClassMenuId != null)
            {
                await Task.Delay(10); // Small delay to ensure DOM is updated
                var dropdownId = $"class-dropdown-{className.Replace(" ", "-")}";
                var buttonId = $"class-menu-btn-{className.Replace(" ", "-")}";
                await JSRuntime.InvokeVoidAsync("positionDropdown", dropdownId, buttonId);

                // Setup outside click detection
                var dotNetRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("setupDropdownOutsideClick", dropdownId, dotNetRef);
            }
        }

        [JSInvokable]
        public async Task CloseDropdown()
        {
            _openClassMenuId = null;
            _openAnnotationMenuId = null;
            await InvokeAsync(StateHasChanged);
            await JSRuntime.InvokeVoidAsync("cleanupDropdownListener");
        }

        private void ShowEditClassModal(ProjectClassDto classItem)
        {
            _modalManager.Show(ModalManager.ModalKeys.EditClass);
            _editClassModalRef?.Initialize(classItem.Id, classItem.Name, classItem.Color);
        }

        private void HideEditClassModal() => _modalManager.Hide(ModalManager.ModalKeys.EditClass);

        private async Task DeleteClassWithConfirm(ProjectClassDto classItem)
        {
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
                $"Are you sure you want to delete the class '{classItem.Name}'? All associated annotations will be removed.");

            if (confirmed)
            {
                await DeleteClass(classItem);
                _openClassMenuId = null;
            }
        }

        /// <summary>
        /// Handles class modification using Coordinator
        /// </summary>
        private async void HandleClassModified(EditClassModal.Model model)
        {
            if (string.IsNullOrWhiteSpace(model.ClassName)) return;

            try
            {
                var result = await _coordinator.UpdateClassAsync(model.Id, model.ClassName, model.ClassColor);

                if (!result.IsSuccess)
                {
                    ParseAndShowValidationErrors(result.Error, _editClassModalRef);
                    return;
                }

                HideEditClassModal();
                ShowToast("Success", $"Class '{model.ClassName}' updated successfully.", "success");
                await InvokeAsync(StateHasChanged);
            }
            catch (Application.Common.Exceptions.ValidationException ex)
            {
                ParseAndShowValidationErrors(ex, _editClassModalRef);
            }
        }

        private void ParseAndShowValidationErrors(Application.Common.Exceptions.ValidationException ex, EditClassModal? modalRef)
        {
            foreach (var error in ex.Errors)
            {
                var errorMessage = string.Join(" ", error.Value);
                if (error.Key == "Name")
                {
                    modalRef?.SetValidationError("Name", errorMessage);
                }
                else if (error.Key == "Color")
                {
                    modalRef?.SetValidationError("Color", errorMessage);
                }
            }
        }

        /// <summary>
        /// Deletes a class using Coordinator
        /// </summary>
        private async Task DeleteClass(ProjectClassDto classToDelete)
        {
            var result = await _coordinator.DeleteClassAsync(classToDelete);

            if (!result.IsSuccess)
            {
                ShowToast("Error", $"Failed to delete class: {result.Error}", "error");
                return;
            }

            // Remove annotations with this class from current image
            if (_annotations != null)
            {
                _annotations.RemoveAll(a => a.classDto?.Id == classToDelete.Id);
            }

            _annotationService.NotifyAnnotationsByClassDeleted(classToDelete);
            ShowToast("Success", $"Class '{classToDelete.Name}' deleted successfully.", "success");
            await InvokeAsync(StateHasChanged);
        }

        // Annotation management methods
        private void SwitchToLabelTab()
        {
            // If CanLabel is false, force switch to Process tab
            if (!_workspaceService.CanLabel)
            {
                _activeTab = "Process";
                return;
            }

            _activeTab = "Label";
            // Auto-select first class if none selected
            if (_classItems.Count > 0)
            {
                _drawingToolService.SelectClass(_classItems[0]);
            }
        }

        private void SelectTool(LabelMode tool)
        {
            // For Classification (0) and Anomaly Detection (3) projects, only allow Select and Move modes
            var projectType = _workspaceService.CurrentProjectType?.Value;
            if (projectType == 0 || projectType == 3)
            {
                // Only allow Select and Move tools
                if (tool != LabelMode.Select && tool != LabelMode.Move)
                {
                    return; // Ignore the tool change
                }
            }

            // Cannot use BoundingBox or Segmentation mode if there are no project classes
            if ((tool == LabelMode.BoundingBox || tool == LabelMode.Segmentation) && _classItems.Count == 0)
            {
                _logger.LogWarning("[RightSideBar] Cannot switch to {Tool} mode: no project classes available", tool);
                ShowToast("No Classes", "Please create at least one class before using annotation tools.", "warning");
                return; // Ignore the tool change
            }

            _drawingToolService.SetMode(tool);
        }

        private void HandleAnnotationItemClick(int id, MouseEventArgs e)
        {
            if (e.CtrlKey)
            {
                // Ctrl+Click: toggle this annotation in the selection
                if (_selectedAnnotationIds.Contains(id))
                {
                    _selectedAnnotationIds.Remove(id);
                }
                else
                {
                    _selectedAnnotationIds.Add(id);
                }
            }
            else
            {
                // Regular click: clear selection and select only this one
                _selectedAnnotationIds.Clear();
                _selectedAnnotationIds.Add(id);
            }
            // Notify canvas with ALL selected annotation IDs so it can sync
            _annotationService.SelectMultiple(new HashSet<int>(_selectedAnnotationIds));
        }

        private async void ToggleAnnotationMenu(int annotationId)
        {
            if (_openAnnotationMenuId == annotationId)
            {
                _openAnnotationMenuId = null;
                await JSRuntime.InvokeVoidAsync("cleanupDropdownListener");
            }
            else
            {
                _openAnnotationMenuId = annotationId;
            }
            await InvokeAsync(StateHasChanged);

            // Position the dropdown after render
            if (_openAnnotationMenuId != null)
            {
                await Task.Delay(10); // Small delay to ensure DOM is updated
                var dropdownId = $"annotation-dropdown-{annotationId}";
                var buttonId = $"annotation-menu-btn-{annotationId}";
                await JSRuntime.InvokeVoidAsync("positionDropdown", dropdownId, buttonId);

                // Setup outside click detection
                var dotNetRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("setupDropdownOutsideClick", dropdownId, dotNetRef);
            }
        }

        /// <summary>
        /// Deletes an annotation using Coordinator
        /// </summary>
        private async void DeleteAnnotation(int id)
        {
            var result = await _coordinator.DeleteAnnotationAsync(id);

            if (!result.IsSuccess)
            {
                ShowToast("Error", $"Failed to delete annotation: {result.Error}", "error");
                return;
            }

            if (_annotations != null)
            {
                _annotations.RemoveAll(a => a.Id == id);
            }
            _selectedAnnotationIds.Remove(id);
            _openAnnotationMenuId = null;

            // Coordinator already handles deletion - no need to call StateService again
            await InvokeAsync(StateHasChanged);
        }

        private void ClearAllAnnotations()
        {
            _annotations?.Clear();
            _selectedAnnotationIds.Clear();
            _annotationService.NotifyAllAnnotationsCleared();
        }

        private async void HandleRandomSplit()
        {
            try
            {
                var result = await _coordinator.PerformRandomSplitAsync();
                if (result.IsSuccess && result.Value != null)
                {
                    _toastRef?.Show("Success", result.Value.Message, "success", 3000);
                }
                else
                {
                    _toastRef?.Show("Error", result.Error ?? "Failed to perform random split", "error", 4000);
                }
            }
            catch (Exception ex)
            {
                _toastRef?.Show("Error", $"An error occurred: {ex.Message}", "error", 4000);
            }
        }

        private void ImportPreviousLabels()
        {
            _annotationService.TriggerImportPreviousLabels();
        }
        private void ShowToast(string title, string message, string type)
        {
            _toastRef?.Show(title, message, type);
        }

        /// <summary>
        /// Handles the Initialize Project button click
        /// Removes all annotations and roles for the current project
        /// </summary>
        private async Task HandleInitializeProject()
        {
            // Confirm action with user
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
                "This will remove ALL annotations and roles from this project. This action cannot be undone. Continue?");

            if (!confirmed)
            {
                return;
            }

            try
            {
                var result = await _coordinator.InitializeProjectAsync();

                if (result.IsSuccess && result.Value != null)
                {
                    // Show success message with details
                    var message = $"Project initialized successfully\n" +
                                  $"• {result.Value.AnnotationsDeleted} annotations removed\n" +
                                  $"• {result.Value.RolesDeleted} roles removed\n" +
                                  $"• {result.Value.ImagesAffected} images affected";

                    ShowToast("Success", message, "success");

                    // Clear all local annotations after initialization
                    _annotations?.Clear();
                    _selectedAnnotationIds.Clear();
                    _openAnnotationMenuId = null;

                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    ShowToast("Error", result.Error ?? "Failed to initialize project", "error");
                }
            }
            catch (Exception ex)
            {
                ShowToast("Error", $"An error occurred: {ex.Message}", "error");
            }
        }
        private async void HandleNavigateNextPage()
        {
            if (_workspaceService.CurrentProjectId.HasValue)
            {
                NavigationManager.NavigateTo($"/wheeldl/training/{_workspaceService.CurrentProjectId.Value}");
            }
        }

        private void ToggleTrainingDetails(int trainingId)
        {
            if (_expandedTrainingIds.Contains(trainingId))
            {
                _expandedTrainingIds.Remove(trainingId);
            }
            else
            {
                _expandedTrainingIds.Add(trainingId);
            }
        }

        private bool IsTrainingExpanded(int trainingId)
        {
            return _expandedTrainingIds.Contains(trainingId);
        }
    }

}
