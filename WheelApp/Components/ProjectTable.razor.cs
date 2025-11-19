using Microsoft.AspNetCore.Components;
using WheelApp.Application.DTOs;
using WheelApp.State;

namespace WheelApp.Components
{
    /// <summary>
    /// Project table component - displays projects in a grid with pagination
    /// Handles client-side pagination and selection
    /// </summary>
    public partial class ProjectTable : ComponentBase
    {
        /// <summary>
        /// Full list of projects (client-side pagination)
        /// </summary>
        [Parameter]
        public List<ProjectDto> Projects { get; set; } = new();

        /// <summary>
        /// Selection state (managed externally)
        /// </summary>
        [Parameter]
        public SelectionState<int> SelectionState { get; set; } = new();

        /// <summary>
        /// Pagination state (managed externally)
        /// </summary>
        [Parameter]
        public PaginationState PaginationState { get; set; } = new(pageSize: 3);

        /// <summary>
        /// Event fired when a row is double-clicked
        /// </summary>
        [Parameter]
        public EventCallback<int> OnRowDoubleClick { get; set; }

        /// <summary>
        /// Event fired when the edit button is clicked
        /// </summary>
        [Parameter]
        public EventCallback<int> OnEditClick { get; set; }

        /// <summary>
        /// Event fired when the page is changed
        /// </summary>
        [Parameter]
        public EventCallback<int> OnPageChange { get; set; }

        /// <summary>
        /// Event fired when a row's selection is toggled
        /// </summary>
        [Parameter]
        public EventCallback<int> OnRowSelectionToggle { get; set; }

        /// <summary>
        /// Event fired when all rows are toggled
        /// </summary>
        [Parameter]
        public EventCallback OnToggleAllRows { get; set; }

        /// <summary>
        /// Get paginated projects for current page
        /// If using server-side pagination (OnPageChange is set), Projects already contains only the current page
        /// If using client-side pagination, we need to Skip/Take
        /// </summary>
        private List<ProjectDto> PaginatedProjects
        {
            get
            {
                if (OnPageChange.HasDelegate)
                {
                    // Server-side pagination - Projects already contains only current page data
                    return Projects;
                }
                else
                {
                    // Client-side pagination - need to Skip/Take
                    return Projects
                        .Skip((PaginationState.CurrentPage - 1) * PaginationState.PageSize)
                        .Take(PaginationState.PageSize)
                        .ToList();
                }
            }
        }

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
        private bool IsAllRowsSelected => PaginatedProjects.Any() && PaginatedProjects.All(p => SelectionState.IsSelected(p.Id));

        /// <summary>
        /// Handle toggle all checkbox click - invokes EventCallback to let parent handle it
        /// </summary>
        private async Task HandleToggleAll()
        {
            if (OnToggleAllRows.HasDelegate)
            {
                await OnToggleAllRows.InvokeAsync();
            }
        }

        /// <summary>
        /// Change to a different page
        /// </summary>
        private async Task ChangePage(int pageNumber)
        {
            if (OnPageChange.HasDelegate)
            {
                // Server-side pagination - let parent handle it
                await OnPageChange.InvokeAsync(pageNumber);
            }
            else
            {
                // Client-side pagination - handle internally
                PaginationState.GoToPage(pageNumber);
            }
        }
    }
}
