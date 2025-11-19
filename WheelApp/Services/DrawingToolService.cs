using WheelApp.Application.DTOs;

namespace WheelApp.Services;

/// <summary>
/// Label mode enumeration for annotation tools
/// </summary>
public enum LabelMode
{
    Move,
    Select,
    BoundingBox,
    Segmentation
}

/// <summary>
/// Drawing tool and mode service
/// Manages current drawing mode and selected class
/// Single Responsibility: Drawing tool management only
/// </summary>
public class DrawingToolService
{
    /// <summary>
    /// Current drawing mode
    /// </summary>
    public LabelMode CurrentMode { get; private set; } = LabelMode.Move;

    /// <summary>
    /// Currently selected class for annotation
    /// </summary>
    public ProjectClassDto? SelectedClass { get; private set; }

    /// <summary>
    /// Whether label mode (BBox/Segmentation) can be used
    /// Requires at least one class to be available
    /// </summary>
    public bool CanUseLabelMode => SelectedClass != null;

    /// <summary>
    /// Event fired when drawing mode changes
    /// </summary>
    public event Action<LabelMode>? OnModeChanged;

    /// <summary>
    /// Event fired when a class is selected
    /// </summary>
    public event Action<ProjectClassDto>? OnClassSelected;

    /// <summary>
    /// Event fired when tool is selected (for backward compatibility)
    /// </summary>
    public event Action<LabelMode>? OnToolSelected;

    /// <summary>
    /// Event fired when drawing tool is selected (for backward compatibility)
    /// </summary>
    public event Action<LabelMode>? OnDrawingToolSelected;

    /// <summary>
    /// Sets the current drawing mode with validation
    /// BoundingBox and Segmentation modes require a selected class
    /// </summary>
    public void SetMode(LabelMode mode)
    {
        // Validate: BBox and Segmentation require a selected class
        if (mode == LabelMode.BoundingBox || mode == LabelMode.Segmentation)
        {
            if (!CanUseLabelMode)
            {
                // Cannot switch to label mode without a class
                return;
            }
        }

        if (CurrentMode != mode)
        {
            CurrentMode = mode;
            OnModeChanged?.Invoke(mode);
            OnToolSelected?.Invoke(mode);
            OnDrawingToolSelected?.Invoke(mode);
        }
    }

    /// <summary>
    /// Selects a class for annotation
    /// </summary>
    public void SelectClass(ProjectClassDto projectClass)
    {
        if (SelectedClass == null || SelectedClass.Id != projectClass.Id)
        {
            SelectedClass = projectClass;
            OnClassSelected?.Invoke(projectClass);
        }
    }

    /// <summary>
    /// Sets the selected class without triggering events
    /// Used for auto-selection scenarios (e.g., after class creation)
    /// </summary>
    public void SetSelectedClassSilently(ProjectClassDto projectClass)
    {
        SelectedClass = projectClass;
    }

    /// <summary>
    /// Clears the selected class
    /// </summary>
    public void ClearSelectedClass()
    {
        SelectedClass = null;
    }

    /// <summary>
    /// Resets to default mode (Select)
    /// </summary>
    public void ResetMode()
    {
        SetMode(LabelMode.Select);
    }

    /// <summary>
    /// Resets to Move mode if currently in BoundingBox or Segmentation mode
    /// This is called when a class is deleted
    /// </summary>
    public void ResetModeIfDrawing()
    {
        if (CurrentMode == LabelMode.BoundingBox || CurrentMode == LabelMode.Segmentation)
        {
            CurrentMode = LabelMode.Move;
            OnModeChanged?.Invoke(LabelMode.Move);
            OnToolSelected?.Invoke(LabelMode.Move);
            OnDrawingToolSelected?.Invoke(LabelMode.Move);
        }
    }

}
