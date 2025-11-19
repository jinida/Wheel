using WheelApp.Application.DTOs;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for image selection and multi-selection management
    /// Phase 2 Refactoring - Delegates to ImageSelectionService
    /// </summary>
    public class ProjectImageSelectionCoordinator
    {
        private readonly ImageSelectionService _imageSelectionService;

        public ProjectImageSelectionCoordinator(ImageSelectionService imageSelectionService)
        {
            _imageSelectionService = imageSelectionService;
        }

        /// <summary>
        /// Gets the currently selected image
        /// </summary>
        public ImageDto? CurrentImage => _imageSelectionService.SelectedImage;
        public ImageDto? PreviousImage => _imageSelectionService.PreviousSelectedImage;
        /// <summary>
        /// Gets all selected image IDs
        /// </summary>
        public HashSet<int> SelectedImageIds => _imageSelectionService.SelectedImageIds;

        /// <summary>
        /// Toggles image selection with Ctrl/Shift support
        /// </summary>
        public void ToggleImageSelection(ImageDto image, bool isCtrlPressed, bool isShiftPressed, List<ImageDto> sortedImages)
        {
            if (isShiftPressed && _imageSelectionService.AnchorImageId.HasValue)
            {
                SelectImageRange(_imageSelectionService.AnchorImageId.Value, image.Id, sortedImages);
            }
            else if (isCtrlPressed)
            {
                _imageSelectionService.ToggleSelection(image.Id);
                _imageSelectionService.SelectImage(image);
                _imageSelectionService.SetAnchor(image.Id);
            }
            else
            {
                // Normal click: Single selection
                _imageSelectionService.ClearSelection();
                _imageSelectionService.SelectedImageIds.Add(image.Id);
                _imageSelectionService.SelectImage(image);
                _imageSelectionService.SetAnchor(image.Id);
            }
        }

        /// <summary>
        /// Selects all images between two IDs
        /// </summary>
        public void SelectImageRange(int fromImageId, int toImageId, List<ImageDto> sortedImages)
        {
            var fromIdx = sortedImages.FindIndex(i => i.Id == fromImageId);
            var toIdx = sortedImages.FindIndex(i => i.Id == toImageId);

            if (fromIdx != -1 && toIdx != -1)
            {
                var selectedIds = sortedImages
                    .Skip(Math.Min(fromIdx, toIdx))
                    .Take(Math.Abs(toIdx - fromIdx) + 1)
                    .Select(i => i.Id)
                    .ToHashSet();

                _imageSelectionService.SelectMultiple(selectedIds);
                _imageSelectionService.SelectImage(sortedImages[toIdx]);
            }
        }

        /// <summary>
        /// Clears all selections
        /// </summary>
        public void ClearSelection()
        {
            _imageSelectionService.ClearSelection();
        }

        public void ClearAll()
        {
            _imageSelectionService.ClearAll();
        }

        /// <summary>
        /// Gets list of selected images from sorted list
        /// </summary>
        public List<ImageDto> GetSelectedImages(List<ImageDto> sortedImages)
        {
            return sortedImages.Where(i => _imageSelectionService.SelectedImageIds.Contains(i.Id)).ToList();
        }

        /// <summary>
        /// Navigates to image by offset (arrow key navigation)
        /// </summary>
        public void NavigateImage(int offset, List<ImageDto> sortedImages, bool shiftPressed)
        {
            if (!sortedImages.Any()) return;

            var currentIdx = _imageSelectionService.SelectedImage != null
                ? sortedImages.FindIndex(i => i.Id == _imageSelectionService.SelectedImage.Id)
                : 0;

            var newIdx = Math.Clamp(currentIdx + offset, 0, sortedImages.Count - 1);
            var newImage = sortedImages[newIdx];

            if (shiftPressed)
            {
                // Extend selection
                if (_imageSelectionService.AnchorImageId.HasValue)
                {
                    SelectImageRange(_imageSelectionService.AnchorImageId.Value, newImage.Id, sortedImages);
                }
            }
            else
            {
                _imageSelectionService.ClearSelection();
                _imageSelectionService.SelectedImageIds.Add(newImage.Id);
                _imageSelectionService.SelectImage(newImage);
                _imageSelectionService.SetAnchor(newImage.Id);
            }
        }

        /// <summary>
        /// Selects all images
        /// </summary>
        public void SelectAllImages(List<ImageDto> sortedImages)
        {
            if (!sortedImages.Any()) return;

            var allIds = sortedImages.Select(i => i.Id).ToHashSet();
            _imageSelectionService.SelectMultiple(allIds);
            _imageSelectionService.SelectImage(sortedImages.First());
        }
    }
}
