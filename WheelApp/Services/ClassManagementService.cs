using WheelApp.Application.DTOs;

namespace WheelApp.Services;

/// <summary>
/// Class management service
/// Manages class creation/deletion events and pending selections
/// Single Responsibility: Class management only
/// </summary>
public class ClassManagementService
{
    /// <summary>
    /// Tracks the name of a newly created class that should be auto-selected
    /// after workspace reload completes
    /// </summary>
    public string? PendingClassSelection { get; set; }

    /// <summary>
    /// Event fired when classes have changed (created/updated/deleted)
    /// Triggers workspace reload
    /// </summary>
    public event Action? OnClassesChanged;

    /// <summary>
    /// Event fired when a class is deleted
    /// Triggers UI updates
    /// </summary>
    public event Action? OnClassDeleted;

    /// <summary>
    /// Event fired when class colors are updated
    /// </summary>
    public event Action<List<ProjectClassDto>>? OnClassColorsUpdated;

    /// <summary>
    /// Notifies that classes have changed
    /// Should be called after Create/Update/Delete class operations
    /// </summary>
    public void NotifyClassesChanged()
    {
        OnClassesChanged?.Invoke();
    }

    /// <summary>
    /// Notifies that a class was deleted
    /// </summary>
    public void NotifyClassDeleted()
    {
        OnClassDeleted?.Invoke();
    }

    /// <summary>
    /// Updates class colors and notifies subscribers
    /// </summary>
    public void UpdateClassColors(List<ProjectClassDto> projectClasses)
    {
        OnClassColorsUpdated?.Invoke(projectClasses);
    }

    /// <summary>
    /// Clears pending class selection
    /// </summary>
    public void ClearPendingSelection()
    {
        PendingClassSelection = null;
    }
}
