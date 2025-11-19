using WheelApp.Application.DTOs;

namespace WheelApp.Services;

/// <summary>
/// Workspace data service
/// Manages read-only workspace data (ProjectWorkspaceDto)
/// Single Responsibility: Workspace data only
/// </summary>
public class ProjectWorkspaceService
{
    /// <summary>
    /// Current workspace data (read-only from database)
    /// </summary>
    public ProjectWorkspaceDto? CurrentWorkspace { get; private set; }

    /// <summary>
    /// Current project ID
    /// </summary>
    public int? CurrentProjectId { get; private set; }

    /// <summary>
    /// Current project type
    /// </summary>
    public ProjectTypeDto? CurrentProjectType { get; private set; }

    /// <summary>
    /// Whether labeling is currently allowed
    /// </summary>
    public bool CanLabel { get; private set; }

    /// <summary>
    /// Project classes from workspace
    /// </summary>
    public List<ProjectClassDto> ProjectClasses => CurrentWorkspace?.ProjectClasses ?? new();

    /// <summary>
    /// Images from workspace
    /// </summary>
    public List<ImageDto> Images => CurrentWorkspace?.Images ?? new();

    /// <summary>
    /// Flatten all annotations from all images
    /// </summary>
    public List<AnnotationDto> Annotations => CurrentWorkspace?.Images
        .SelectMany(img => img.Annotation)
        .ToList() ?? new();

    /// <summary>
    /// Event fired when workspace data is loaded or updated
    /// </summary>
    public event Action? OnWorkspaceLoaded;

    /// <summary>
    /// Event fired when dataset changes (for backward compatibility)
    /// </summary>
    public event Action? OnDatasetChanged;

    /// <summary>
    /// Updates the entire workspace data
    /// Should be called after loading workspace from database
    /// </summary>
    public void UpdateWorkspace(ProjectWorkspaceDto workspace)
    {
        CurrentWorkspace = workspace;
        OnWorkspaceLoaded?.Invoke();
    }

    /// <summary>
    /// Sets the current project ID
    /// </summary>
    public void SetCurrentProject(int? projectId)
    {
        if (CurrentProjectId != projectId)
        {
            CurrentProjectId = projectId;
        }
    }

    /// <summary>
    /// Sets the current project type
    /// </summary>
    public void SetProjectType(ProjectTypeDto? projectType)
    {
        if (CurrentProjectType != projectType)
        {
            CurrentProjectType = projectType;
        }
    }

    /// <summary>
    /// Sets whether labeling is currently allowed
    /// </summary>
    public void SetLabelability(bool canLabel)
    {
        if (CanLabel != canLabel)
        {
            CanLabel = canLabel;
        }
    }

    /// <summary>
    /// Clears workspace data when project is deselected
    /// </summary>
    public void ClearWorkspace()
    {
        CurrentWorkspace = null;
        CurrentProjectId = null;
        CurrentProjectType = null;
        CanLabel = false;
    }

    /// <summary>
    /// Notifies that dataset has changed (for backward compatibility)
    /// </summary>
    public void NotifyDatasetChanged()
    {
        OnDatasetChanged?.Invoke();
    }

    /// <summary>
    /// Triggers OnWorkspaceLoaded event without reloading data
    /// Use when workspace data in memory was updated and UI needs to refresh
    /// </summary>
    public void TriggerWorkspaceLoaded()
    {
        OnWorkspaceLoaded?.Invoke();
    }
}
