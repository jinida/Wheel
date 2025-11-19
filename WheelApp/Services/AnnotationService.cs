using WheelApp.Application.DTOs;

namespace WheelApp.Services;

/// <summary>
/// Annotation selection and management service
/// Manages annotation selection and deletion events
/// Single Responsibility: Annotation management only
/// </summary>
public class AnnotationService
{
    /// <summary>
    /// Set of selected annotation IDs
    /// </summary>
    public HashSet<int> SelectedAnnotationIds { get; private set; } = new();

    /// <summary>
    /// Event fired when a single annotation is selected
    /// </summary>
    public event Action<int>? OnAnnotationSelected;

    /// <summary>
    /// Event fired when multiple annotations are selected
    /// </summary>
    public event Action<HashSet<int>>? OnMultipleAnnotationsSelected;

    /// <summary>
    /// Event fired when an annotation is deleted
    /// </summary>
    public event Action<int>? OnAnnotationDeleted;

    /// <summary>
    /// Event fired when all annotations are cleared
    /// </summary>
    public event Action? OnAllAnnotationsCleared;

    /// <summary>
    /// Event fired when annotations of a specific class are deleted
    /// </summary>
    public event Action<ProjectClassDto>? OnAnnotationsByClassDeleted;

    /// <summary>
    /// Event fired when an annotation is added (for UI update)
    /// </summary>
    public event Action<int, string, string>? OnAnnotationAdded;

    /// <summary>
    /// Event fired when selected annotations' class is changed
    /// </summary>
    public event Action<List<int>, int>? OnAnnotationsClassChanged;

    /// <summary>
    /// Event fired when import previous labels is requested
    /// </summary>
    public event Action? OnImportPreviousLabels;

    /// <summary>
    /// Event fired when annotations are added to images (for batch import operations)
    /// Parameters: List of image IDs that received new annotations
    /// </summary>
    public event Action<List<int>>? OnImageAnnotationsUpdated;

    public event Action<int>? OnSplitRoleSelected;

    /// <summary>
    /// Selects a single annotation
    /// </summary>
    public void SelectAnnotation(int id)
    {
        SelectedAnnotationIds.Clear();
        SelectedAnnotationIds.Add(id);
        OnAnnotationSelected?.Invoke(id);
    }

    /// <summary>
    /// Selects multiple annotations
    /// </summary>
    public void SelectMultiple(HashSet<int> ids)
    {
        SelectedAnnotationIds.Clear();
        foreach (var id in ids)
        {
            SelectedAnnotationIds.Add(id);
        }
        OnMultipleAnnotationsSelected?.Invoke(ids);
    }

    /// <summary>
    /// Clears all annotation selections
    /// </summary>
    public void ClearSelection()
    {
        SelectedAnnotationIds.Clear();
    }

    /// <summary>
    /// Notifies that an annotation was deleted
    /// </summary>
    public void NotifyAnnotationDeleted(int id)
    {
        OnAnnotationDeleted?.Invoke(id);
    }

    /// <summary>
    /// Notifies that all annotations were cleared
    /// </summary>
    public void NotifyAllAnnotationsCleared()
    {
        OnAllAnnotationsCleared?.Invoke();
    }

    /// <summary>
    /// Notifies that annotations of a specific class were deleted
    /// </summary>
    public void NotifyAnnotationsByClassDeleted(ProjectClassDto projectClass)
    {
        OnAnnotationsByClassDeleted?.Invoke(projectClass);
    }

    /// <summary>
    /// Notifies that an annotation was added
    /// </summary>
    public void NotifyAnnotationAdded(int id, string type, string className)
    {
        OnAnnotationAdded?.Invoke(id, type, className);
    }

    /// <summary>
    /// Notifies that selected annotations' class was changed
    /// </summary>
    public void NotifyAnnotationsClassChanged(List<int> annotationIds, int newClassId)
    {
        OnAnnotationsClassChanged?.Invoke(annotationIds, newClassId);
    }

    /// <summary>
    /// Triggers import of labels from previous image
    /// </summary>
    public void TriggerImportPreviousLabels()
    {
        OnImportPreviousLabels?.Invoke();
    }

    /// <summary>
    /// Notifies that annotations were added to specific images
    /// </summary>
    public void NotifyImageAnnotationsUpdated(List<int> imageIds)
    {
        OnImageAnnotationsUpdated?.Invoke(imageIds);
    }

    /// <summary>
    /// Selects a split role (Train/Validation/Test)
    /// </summary>
    public void SelectSplitRole(int roleType) => OnSplitRoleSelected?.Invoke(roleType);
}
