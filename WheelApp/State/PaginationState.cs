namespace WheelApp.State;

/// <summary>
/// Pagination state management
/// Manages current page, page size, and total counts
/// </summary>
public class PaginationState
{
    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalCount = 0;

    /// <summary>
    /// Page size (items per page)
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage => _currentPage;

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => _totalPages;

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount => _totalCount;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => _currentPage > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => _currentPage < _totalPages;

    public PaginationState(int pageSize = 10)
    {
        PageSize = pageSize;
    }

    /// <summary>
    /// Updates pagination state based on total count and total pages
    /// Returns true if current page was corrected (out of bounds)
    /// </summary>
    public bool UpdatePagination(int totalCount, int totalPages)
    {
        _totalCount = totalCount;
        _totalPages = Math.Max(1, totalPages);

        // Correct current page if out of bounds
        if (_currentPage > _totalPages)
        {
            _currentPage = _totalPages;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Goes to a specific page number (1-based)
    /// Returns true if the page changed
    /// </summary>
    public bool GoToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > _totalPages || pageNumber == _currentPage)
        {
            return false;
        }

        _currentPage = pageNumber;
        return true;
    }

    /// <summary>
    /// Goes to the next page
    /// Returns true if moved
    /// </summary>
    public bool GoToNextPage()
    {
        return GoToPage(_currentPage + 1);
    }

    /// <summary>
    /// Goes to the previous page
    /// Returns true if moved
    /// </summary>
    public bool GoToPreviousPage()
    {
        return GoToPage(_currentPage - 1);
    }

    /// <summary>
    /// Goes to the first page
    /// Returns true if moved
    /// </summary>
    public bool GoToFirstPage()
    {
        return GoToPage(1);
    }

    /// <summary>
    /// Goes to the last page
    /// Returns true if moved
    /// </summary>
    public bool GoToLastPage()
    {
        return GoToPage(_totalPages);
    }

    /// <summary>
    /// Resets pagination to first page
    /// </summary>
    public void Reset()
    {
        _currentPage = 1;
        _totalPages = 1;
        _totalCount = 0;
    }
}
