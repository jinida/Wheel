using WheelApp.Application.DTOs;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Helper for image grid sorting and filtering
    /// Phase 2 Refactoring - Pure logic helper with no dependencies
    /// Can be instantiated directly with 'new' - no DI registration needed
    /// </summary>
    public class GridSortHelper
    {
        private string? _sortColumn = "FileName";
        private bool _sortAscending = true;

        /// <summary>
        /// Current sort column
        /// </summary>
        public string? SortColumn => _sortColumn;

        /// <summary>
        /// Current sort direction (true = ascending, false = descending)
        /// </summary>
        public bool SortAscending => _sortAscending;

        /// <summary>
        /// Sets the sort column and toggles direction if same column
        /// </summary>
        public void SetSortColumn(string columnName)
        {
            if (_sortColumn == columnName)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = columnName;
                _sortAscending = true;
            }
        }

        /// <summary>
        /// Toggles the sort direction
        /// </summary>
        public void ToggleSortDirection()
        {
            _sortAscending = !_sortAscending;
        }

        /// <summary>
        /// Sorts images based on current sort settings
        /// Returns IEnumerable instead of materializing to List
        /// </summary>
        public IEnumerable<ImageDto> GetSortedImages(List<ImageDto> images)
        {
            if (_sortColumn == null) return images;

            IEnumerable<ImageDto> sorted = _sortColumn switch
            {
                "FileName" => _sortAscending
                    ? images.OrderBy(i => i.Name)
                    : images.OrderByDescending(i => i.Name),

                "Label" => _sortAscending
                    ? images.OrderBy(i => i.Annotation.FirstOrDefault()?.classDto?.Name ?? "")
                    : images.OrderByDescending(i => i.Annotation.FirstOrDefault()?.classDto?.Name ?? ""),

                "Role" => _sortAscending
                    ? images.OrderBy(i => i.RoleType?.Value ?? 0)
                    : images.OrderByDescending(i => i.RoleType?.Value ?? 0),

                "CreateDate" => _sortAscending
                    ? images.OrderBy(i => i.CreatedAt)
                    : images.OrderByDescending(i => i.CreatedAt),

                _ => images
            };

            // Return IEnumerable to avoid unnecessary .ToList() call
            return sorted;
        }

        /// <summary>
        /// Resets sort to default (FileName, ascending)
        /// </summary>
        public void ResetSort()
        {
            _sortColumn = "FileName";
            _sortAscending = true;
        }
    }
}
