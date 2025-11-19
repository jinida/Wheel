using Microsoft.AspNetCore.Components;
using WheelApp.Application.DTOs;
using WheelApp.State;

namespace WheelApp.Components
{
    /// <summary>
    /// Dataset table component - displays datasets in a grid with pagination
    /// Manages its own selection state
    /// </summary>
    public partial class DatasetTable : ComponentBase
    {
        /// <summary>
        /// List of datasets to display
        /// </summary>
        [Parameter]
        public List<DatasetDto> Datasets { get; set; } = new();

        /// <summary>
        /// Selection state (managed externally)
        /// </summary>
        [Parameter]
        public SelectionState<int> SelectionState { get; set; } = new();

        /// <summary>
        /// Current page number for pagination
        /// </summary>
        [Parameter]
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// Total number of pages
        /// </summary>
        [Parameter]
        public int TotalPages { get; set; } = 1;

        /// <summary>
        /// Whether to show pagination controls
        /// </summary>
        [Parameter]
        public bool ShowPagination { get; set; } = true;

        /// <summary>
        /// Event fired when a row is double-clicked
        /// </summary>
        [Parameter]
        public EventCallback<int> OnRowDoubleClick { get; set; }

        /// <summary>
        /// Event fired when a row selection checkbox is toggled
        /// </summary>
        [Parameter]
        public EventCallback<int> OnRowSelectionToggle { get; set; }

        /// <summary>
        /// Event fired when the "select all" checkbox is toggled
        /// </summary>
        [Parameter]
        public EventCallback OnToggleAllRows { get; set; }

        /// <summary>
        /// Event fired when the edit button is clicked
        /// </summary>
        [Parameter]
        public EventCallback<int> OnEditClick { get; set; }

        /// <summary>
        /// Event fired when page changes
        /// </summary>
        [Parameter]
        public EventCallback<int> OnPageChange { get; set; }

        /// <summary>
        /// Check if a row is selected
        /// </summary>
        private bool IsRowSelected(int id) => SelectionState.IsSelected(id);

        /// <summary>
        /// Check if a row is highlighted (single-selected)
        /// </summary>
        private bool IsRowHighlighted(int id) => SelectionState.HighlightedId == id;

        /// <summary>
        /// Check if all rows on current page are selected
        /// </summary>
        private bool IsAllRowsSelected => Datasets.Any() && Datasets.All(d => SelectionState.IsSelected(d.Id));
    }
}
