using WheelApp.Application.DTOs;
using WheelApp.Services;

namespace WheelApp.Service;

/// <summary>
/// DEPRECATED: Legacy state service - Use individual services instead
/// This service is being refactored into domain-specific services:
/// - ProjectWorkspaceService: Workspace data
/// - ImageSelectionService: Image selection
/// - AnnotationService: Annotation management
/// - DrawingToolService: Drawing tools and modes
/// - ClassManagementService: Class management
///
/// This service now acts as a bridge to the new services for backward compatibility.
/// Will be removed in a future version.
/// </summary>
[Obsolete("Use domain-specific services instead (ProjectWorkspaceService, ImageSelectionService, etc.)")]
public class SidebarStateService
{
    // New services (injected)
    private readonly ProjectWorkspaceService _workspaceState;
    private readonly ImageSelectionService _imageSelectionState;
    private readonly AnnotationService _annotationState;
    private readonly DrawingToolService _drawingToolState;
    private readonly ClassManagementService _classManagementState;

    public SidebarStateService(
        ProjectWorkspaceService workspaceState,
        ImageSelectionService imageSelectionState,
        AnnotationService annotationState,
        DrawingToolService drawingToolState,
        ClassManagementService classManagementState)
    {
        _workspaceState = workspaceState;
        _imageSelectionState = imageSelectionState;
        _annotationState = annotationState;
        _drawingToolState = drawingToolState;
        _classManagementState = classManagementState;

        // Wire up events from new services to legacy events
        _workspaceState.OnWorkspaceLoaded += () => OnStateChange?.Invoke();
        _workspaceState.OnDatasetChanged += () => OnDatasetChanged?.Invoke();
        _imageSelectionState.OnImageSelected += (img) => OnImageSelected?.Invoke(img);
        _drawingToolState.OnClassSelected += (cls) => OnClassSelected?.Invoke(cls);
        _classManagementState.OnClassesChanged += () => OnClassesChanged?.Invoke();
        _classManagementState.OnClassDeleted += () => OnClassDeleted?.Invoke();
        _annotationState.OnAnnotationSelected += (id) => OnAnnotationSelected?.Invoke(id);
        _annotationState.OnMultipleAnnotationsSelected += (ids) => OnMultipleAnnotationsSelected?.Invoke(ids);
        _annotationState.OnAnnotationDeleted += (id) => OnAnnotationDeleted?.Invoke(id);
        _annotationState.OnAllAnnotationsCleared += () => OnAllAnnotationsCleared?.Invoke();
        _annotationState.OnAnnotationsByClassDeleted += (cls) => OnAnnotationsByClassDeleted?.Invoke(cls);
        _annotationState.OnAnnotationAdded += (id, type, name) => OnAnnotationAdded?.Invoke(id, type, name);
        _annotationState.OnAnnotationsClassChanged += (ids, classId) => OnAnnotationsClassChanged?.Invoke(ids, classId);
        _annotationState.OnImportPreviousLabels += () => OnImportPreviousLabels?.Invoke();
        _annotationState.OnImageAnnotationsUpdated += (imageIds) => OnImageAnnotationsUpdated?.Invoke(imageIds);
        _annotationState.OnSplitRoleSelected += (roletype) => OnSplitRoleSelected?.Invoke(roletype);
        _drawingToolState.OnDrawingToolSelected += (mode) => OnDrawingToolSelected?.Invoke(mode);
        _drawingToolState.OnToolSelected += (mode) => OnToolSelected?.Invoke(mode);
        _classManagementState.OnClassColorsUpdated += (classes) => OnClassColorsUpdated?.Invoke(classes);
    }
    // Delegated properties (forward to new state services)
    public ProjectWorkspaceDto? CurrentWorkspace => _workspaceState.CurrentWorkspace;
    public ProjectTypeDto? CurrentProjectType => _workspaceState.CurrentProjectType;
    public bool CanLabel => _workspaceState.CanLabel;
    public int? CurrentProjectId => _workspaceState.CurrentProjectId;
    public ProjectClassDto? CurrentSelectedProjectClass
    {
        get => _drawingToolState.SelectedClass;
        set
        {
            if (value != null)
                _drawingToolState.SelectClass(value);
        }
    }

    public List<ProjectClassDto> ProjectClasses => _workspaceState.ProjectClasses;
    public List<ImageDto> Images => _workspaceState.Images;
    public List<AnnotationDto> Annotations => _workspaceState.Annotations;
    public ImageDto? CurrentImage => _imageSelectionState.SelectedImage;

    public ProjectClassDto? CurrentSelectedClass => _drawingToolState.SelectedClass;

    // Track the name of a newly created class that should be auto-selected after workspace reload
    public string? PendingClassSelection
    {
        get => _classManagementState.PendingClassSelection;
        set => _classManagementState.PendingClassSelection = value;
    }

    // State change events
    public event Action? OnStateChange;
    public event Action? OnDatasetChanged;
    public event Action? OnClassesChanged;
    public event Action? OnClassDeleted;
    public event Action<ImageDto?>? OnImageSelected;

    /// <summary>
    /// Sets whether labeling is currently allowed
    /// </summary>
    public void SetLabelability(bool canLabel)
    {
        _workspaceState.SetLabelability(canLabel);
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the current project type
    /// </summary>
    public void SetTaskType(ProjectTypeDto? projectType)
    {
        _workspaceState.SetProjectType(projectType);
        NotifyStateChanged();
    }

    /// <summary>
    /// Updates the workspace data from ProjectWorkspaceDto
    /// This method should be called from the parent component (Project page)
    /// after loading workspace data via GetProjectWorkspaceQuery
    /// NOTE: This does NOT trigger OnClassesChanged to prevent infinite loops.
    /// OnClassesChanged should only be triggered after actual mutations (Create/Update/Delete).
    /// </summary>
    public void UpdateWorkspaceData(ProjectWorkspaceDto workspace)
    {
        _workspaceState.UpdateWorkspace(workspace);
        UpdateClass(workspace.ProjectClasses);
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the current project ID
    /// </summary>
    public void SetCurrentProject(int? projectId)
    {
        _workspaceState.SetCurrentProject(projectId);
        NotifyStateChanged();
    }

    /// <summary>
    /// Clears workspace data when project is deselected
    /// </summary>
    public void ClearWorkspaceData()
    {
        _workspaceState.ClearWorkspace();
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the currently selected image
    /// </summary>
    public void SetSelectedImage(ImageDto imageDto)
    {
        _imageSelectionState.SelectImage(imageDto);
    }

    /// <summary>
    /// Notifies that classes have changed
    /// </summary>
    public void NotifyClassesChanged() => _classManagementState.NotifyClassesChanged();

    /// <summary>
    /// Notifies that a specific class was deleted
    /// </summary>
    public void NotifyClassDeleted() => _classManagementState.NotifyClassDeleted();

    // Class selection events
    public event Action<ProjectClassDto>? OnClassSelected;

    /// <summary>
    /// Selects a class by name
    /// </summary>
    public void SelectClass(ProjectClassDto projectClass)
    {
        _drawingToolState.SelectClass(projectClass);
    }

    /// <summary>
    /// Sets the current selected class without triggering events
    /// (used for auto-selection scenarios)
    /// </summary>
    public void SetSelectedClassSilently(ProjectClassDto projectClass)
    {
        _drawingToolState.SetSelectedClassSilently(projectClass);
    }

    // Split role selection events
    public event Action<int>? OnSplitRoleSelected;

    /// <summary>
    /// Selects a split role (Train/Validation/Test)
    /// </summary>
    public void SelectSplitRole(int roleType) => _annotationState.SelectSplitRole(roleType);

    public event Action? OnRandomSplit;

    /// <summary>
    /// Triggers random split operation
    /// </summary>
    public void SplitRandomly() => OnRandomSplit?.Invoke();

    public event Action? OnInitializeAll;

    /// <summary>
    /// Triggers initialization of all data
    /// </summary>
    public void InitializeAll() => OnInitializeAll?.Invoke();

    // Annotation-related events
    public event Action<LabelMode>? OnDrawingToolSelected;

    /// <summary>
    /// Selects a drawing tool for annotation
    /// </summary>
    public void SelectDrawingTool(LabelMode tool) => _drawingToolState.SetMode(tool);

    public event Action<int>? OnAnnotationSelected;

    /// <summary>
    /// Selects a single annotation by ID
    /// </summary>
    public void SelectAnnotation(int id) => _annotationState.SelectAnnotation(id);

    public event Action<HashSet<int>>? OnMultipleAnnotationsSelected;

    /// <summary>
    /// Selects multiple annotations by their IDs
    /// </summary>
    public void SelectMultipleAnnotations(HashSet<int> ids) => _annotationState.SelectMultiple(ids);

    public event Action<int>? OnAnnotationDeleted;

    /// <summary>
    /// Notifies that an annotation was deleted
    /// </summary>
    public void DeleteAnnotation(int id) => _annotationState.NotifyAnnotationDeleted(id);

    public event Action? OnAllAnnotationsCleared;

    /// <summary>
    /// Notifies that all annotations were cleared
    /// </summary>
    public void ClearAllAnnotations() => _annotationState.NotifyAllAnnotationsCleared();

    public event Action<ProjectClassDto>? OnAnnotationsByClassDeleted;

    /// <summary>
    /// Notifies that annotations of a specific class were deleted
    /// </summary>
    public void DeleteAnnotationsByClass(ProjectClassDto projectClass)
        => _annotationState.NotifyAnnotationsByClassDeleted(projectClass);

    public event Action<int, string, string>? OnAnnotationAdded;

    /// <summary>
    /// Notifies that an annotation was added
    /// </summary>
    public void NotifyAnnotationAdded(int id, string type, string className)
        => _annotationState.NotifyAnnotationAdded(id, type, className);

    public event Action<List<int>, int>? OnAnnotationsClassChanged;

    /// <summary>
    /// Notifies that selected annotations' class was changed
    /// </summary>
    public void NotifyAnnotationsClassChanged(List<int> annotationIds, int newClassId)
        => _annotationState.NotifyAnnotationsClassChanged(annotationIds, newClassId);

    public event Action<List<ProjectClassDto>>? OnClassColorsUpdated;

    /// <summary>
    /// Updates class colors and notifies subscribers
    /// </summary>
    public void UpdateClass(List<ProjectClassDto> projectClasses)
    {
        _classManagementState.UpdateClassColors(projectClasses);
    }

    public event Action? OnImportPreviousLabels;

    public event Action<List<int>>? OnImageAnnotationsUpdated;

    /// <summary>
    /// Triggers import of labels from previous image
    /// </summary>
    public void ImportPreviousImageLabels() => _annotationState.TriggerImportPreviousLabels();

    /// <summary>
    /// Notifies that annotations were added to specific images
    /// </summary>
    public void NotifyImageAnnotationsUpdated(List<int> imageIds) => _annotationState.NotifyImageAnnotationsUpdated(imageIds);

    public event Action<LabelMode>? OnToolSelected;

    /// <summary>
    /// Notifies that a tool was selected
    /// </summary>
    public void NotifyToolSelected(LabelMode tool)
    {
        _drawingToolState.SetMode(tool);
    }

    /// <summary>
    /// Resets to Move mode if currently in BoundingBox or Segmentation mode
    /// This is called when a class is deleted
    /// </summary>
    private void NotifyStateChanged() => OnStateChange?.Invoke();
}
