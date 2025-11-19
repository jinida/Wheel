namespace WheelApp.State;

/// <summary>
/// Generic selection state management
/// Manages both single highlight (current focus) and multi-selection (checked items)
/// </summary>
/// <typeparam name="T">Type of the ID (typically int)</typeparam>
public class SelectionState<T> where T : struct
{
    private T? _highlightedId;
    private readonly HashSet<T> _selectedIds = new();

    /// <summary>
    /// Currently highlighted (focused) item ID (nullable)
    /// </summary>
    public T? HighlightedId => _highlightedId;

    /// <summary>
    /// Set of selected (checked) item IDs
    /// </summary>
    public IReadOnlySet<T> SelectedIds => _selectedIds;

    /// <summary>
    /// Number of selected items
    /// </summary>
    public int SelectionCount => _selectedIds.Count;

    /// <summary>
    /// Whether any items are selected
    /// </summary>
    public bool HasSelection => _selectedIds.Count > 0;

    /// <summary>
    /// Highlights (focuses) a single item
    /// </summary>
    public void Highlight(T id)
    {
        _highlightedId = id;
    }

    /// <summary>
    /// Clears the highlight
    /// </summary>
    public void ClearHighlight()
    {
        _highlightedId = default;
    }

    /// <summary>
    /// Toggles selection of an item (add if not selected, remove if selected)
    /// </summary>
    public void ToggleSelection(T id)
    {
        if (_selectedIds.Contains(id))
        {
            _selectedIds.Remove(id);
        }
        else
        {
            _selectedIds.Add(id);
        }
    }

    /// <summary>
    /// Adds an item to selection
    /// </summary>
    public void AddToSelection(T id)
    {
        _selectedIds.Add(id);
    }

    /// <summary>
    /// Removes an item from selection
    /// </summary>
    public void RemoveFromSelection(T id)
    {
        _selectedIds.Remove(id);
    }

    /// <summary>
    /// Removes multiple items from selection
    /// </summary>
    public void RemoveFromSelection(IEnumerable<T> ids)
    {
        foreach (var id in ids)
        {
            _selectedIds.Remove(id);
        }
    }

    /// <summary>
    /// Clears all selections
    /// </summary>
    public void ClearSelection()
    {
        _selectedIds.Clear();
    }

    /// <summary>
    /// Checks if an item is selected
    /// </summary>
    public bool IsSelected(T id)
    {
        return _selectedIds.Contains(id);
    }

    /// <summary>
    /// Clears both highlight and selection
    /// </summary>
    public void Clear()
    {
        ClearHighlight();
        ClearSelection();
    }

    /// <summary>
    /// Sets multiple items as selected
    /// </summary>
    public void SetSelection(IEnumerable<T> ids)
    {
        _selectedIds.Clear();
        foreach (var id in ids)
        {
            _selectedIds.Add(id);
        }
    }

    /// <summary>
    /// Toggles selection (for checkbox click)
    /// Alias for ToggleSelection
    /// </summary>
    public void Toggle(T id)
    {
        ToggleSelection(id);
    }

    /// <summary>
    /// Toggles highlight (for row click)
    /// If currently highlighted, clears it. Otherwise highlights it.
    /// </summary>
    public void ToggleHighlight(T id)
    {
        if (_highlightedId != null && _highlightedId.Equals(id))
        {
            ClearHighlight();
        }
        else
        {
            Highlight(id);
        }
    }

    /// <summary>
    /// Toggles all items in the given collection
    /// If all are selected, deselects all. Otherwise, selects all.
    /// </summary>
    public void ToggleAll(IEnumerable<T> allIds)
    {
        var allIdsList = allIds.ToList();
        bool allSelected = allIdsList.All(id => _selectedIds.Contains(id));

        if (allSelected)
        {
            // Deselect all
            foreach (var id in allIdsList)
            {
                _selectedIds.Remove(id);
            }
        }
        else
        {
            // Select all
            foreach (var id in allIdsList)
            {
                _selectedIds.Add(id);
            }
        }
    }
}
