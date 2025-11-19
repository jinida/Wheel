using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WheelApp.Application.DTOs;

namespace WheelApp.Components
{
    /// <summary>
    /// Image table component - displays images in a data grid
    /// Manages sorting and row selection
    /// </summary>
    public partial class ImageTable : ComponentBase, IAsyncDisposable
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        private bool _isInitialized = false;
        /// <summary>
        /// List of images to display
        /// </summary>
        [Parameter]
        public List<ImageDto> Images { get; set; } = new();

        /// <summary>
        /// Currently selected image (highlighted)
        /// </summary>
        [Parameter]
        public ImageDto? SelectedImage { get; set; }

        /// <summary>
        /// Set of selected image IDs (for multi-selection)
        /// </summary>
        [Parameter]
        public HashSet<int> SelectedImageIds { get; set; } = new();

        /// <summary>
        /// Set of animating row IDs (for visual feedback)
        /// </summary>
        [Parameter]
        public HashSet<int> AnimatingRows { get; set; } = new();

        /// <summary>
        /// Current sort column
        /// </summary>
        [Parameter]
        public string? SortColumn { get; set; }

        /// <summary>
        /// Sort direction (true = ascending, false = descending)
        /// </summary>
        [Parameter]
        public bool SortAscending { get; set; } = true;

        /// <summary>
        /// Project type to determine label column name
        /// </summary>
        [Parameter]
        public int? ProjectType { get; set; }

        /// <summary>
        /// Event fired when a row is clicked
        /// </summary>
        [Parameter]
        public EventCallback<(ImageDto Image, MouseEventArgs Event)> OnRowClick { get; set; }

        /// <summary>
        /// Event fired when sort column changes
        /// </summary>
        [Parameter]
        public EventCallback<string> OnSortChanged { get; set; }

        /// <summary>
        /// Gets the label column name based on project type
        /// </summary>
        private string GetLabelColumnName()
        {
            return ProjectType switch
            {
                0 => "Class",           // Classification
                1 => "BBox",            // Detection
                2 => "Segmentation",    // Segmentation
                3 => "Normal/Abnormal", // Anomaly Detection
                _ => "Label"
            };
        }

        /// <summary>
        /// Gets label information from an image
        /// </summary>
        private string GetLabelInfoFromImage(ImageDto image)
        {
            if (image.Annotation == null || !image.Annotation.Any())
                return "-";

            return ProjectType switch
            {
                0 or 3 => image.Annotation.FirstOrDefault()?.classDto?.Name ?? "-", // Classification/Anomaly: show first class
                1 or 2 => GetAnnotationCountByClass(image.Annotation), // Detection/Segmentation: show count per class
                _ => "-"
            };
        }

        /// <summary>
        /// Gets annotation count grouped by class name
        /// Format: "2 Car, 1 Person, 3 Tree"
        /// </summary>
        private string GetAnnotationCountByClass(List<AnnotationDto> annotations)
        {
            var grouped = annotations
                .Where(a => a.classDto != null)
                .GroupBy(a => a.classDto!.Name)
                .Select(g => $"{g.Count()} {g.Key}")
                .ToList();

            return grouped.Any() ? string.Join(", ", grouped) : "-";
        }

        /// <summary>
        /// Handles sort toggle
        /// </summary>
        private async Task ToggleSort(string column)
        {
            await OnSortChanged.InvokeAsync(column);
        }

        /// <summary>
        /// Handles row click
        /// </summary>
        private async Task HandleRowClick(ImageDto image, MouseEventArgs e)
        {
            await OnRowClick.InvokeAsync((image, e));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender || !_isInitialized)
            {
                _isInitialized = true;
                try
                {
                    await JSRuntime.InvokeVoidAsync("wheelApp.initializeColumnResizing", "image-grid-container", true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing column resizing: {ex.Message}");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                // Cleanup column resizing observer
                await JSRuntime.InvokeVoidAsync("wheelApp.cleanupColumnResizing", "image-grid-container");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up column resizing: {ex.Message}");
            }
        }
    }
}
