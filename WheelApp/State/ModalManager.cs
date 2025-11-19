namespace WheelApp.State;

/// <summary>
/// Modal visibility management
/// Manages show/hide state for multiple modals using string keys
/// </summary>
public class ModalManager
{
    private readonly HashSet<string> _visibleModals = new();

    /// <summary>
    /// Common modal keys used throughout the application
    /// </summary>
    public static class ModalKeys
    {
        public const string CreateDataset = "CreateDataset";
        public const string CreateProject = "CreateProject";
        public const string EditDataset = "EditDataset";
        public const string EditProject = "EditProject";
        public const string CreateClass = "CreateClass";
        public const string EditClass = "EditClass";
    }

    /// <summary>
    /// Shows a modal by its key
    /// </summary>
    public void Show(string modalKey)
    {
        _visibleModals.Add(modalKey);
    }

    /// <summary>
    /// Hides a modal by its key
    /// </summary>
    public void Hide(string modalKey)
    {
        _visibleModals.Remove(modalKey);
    }

    /// <summary>
    /// Checks if a modal is currently visible
    /// </summary>
    public bool IsVisible(string modalKey)
    {
        return _visibleModals.Contains(modalKey);
    }

    /// <summary>
    /// Hides all modals
    /// </summary>
    public void HideAll()
    {
        _visibleModals.Clear();
    }
}
