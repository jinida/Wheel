using Microsoft.AspNetCore.Components;

namespace WheelApp.Components
{
    /// <summary>
    /// Reusable pagination component with smart page number display
    /// Shows all pages if total pages <= MaxVisiblePages
    /// Otherwise shows first, last, current page, and surrounding pages with ellipsis
    /// </summary>
    public partial class Pagination : ComponentBase
    {
        /// <summary>
        /// Current active page number
        /// </summary>
        [Parameter]
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// Total number of pages
        /// </summary>
        [Parameter]
        public int TotalPages { get; set; } = 1;

        /// <summary>
        /// Whether to show the pagination controls
        /// </summary>
        [Parameter]
        public bool ShowPagination { get; set; } = true;

        /// <summary>
        /// Maximum number of page buttons to show before using ellipsis
        /// Default is 7 (shows all pages if total <= 7)
        /// </summary>
        [Parameter]
        public int MaxVisiblePages { get; set; } = 7;

        /// <summary>
        /// Number of pages to show around the current page when using ellipsis
        /// Default is 1 (shows current page ± 1)
        /// </summary>
        [Parameter]
        public int PageRangeDisplayed { get; set; } = 1;

        /// <summary>
        /// Event fired when page changes
        /// </summary>
        [Parameter]
        public EventCallback<int> OnPageChange { get; set; }

        /// <summary>
        /// Calculate the starting page number for the visible range
        /// </summary>
        private int StartPage
        {
            get
            {
                var start = CurrentPage - PageRangeDisplayed;
                return Math.Max(2, start); // Never go below 2 (since 1 is always shown)
            }
        }

        /// <summary>
        /// Calculate the ending page number for the visible range
        /// </summary>
        private int EndPage
        {
            get
            {
                var end = CurrentPage + PageRangeDisplayed;
                return Math.Min(TotalPages - 1, end); // Never exceed TotalPages - 1 (since last is always shown)
            }
        }
    }
}
