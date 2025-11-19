using WheelApp.Application.DTOs;

namespace WheelApp.Services;

/// <summary>
/// Image selection service
/// Manages selected images and multi-selection state
/// Single Responsibility: Image selection only
/// </summary>
public class ImageSelectionService
{
    /// <summary>
    /// Currently selected image (highlighted in canvas)
    /// </summary>
    public ImageDto? SelectedImage { get; private set; }

    /// <summary>
    /// Previously selected image (for Import Previous Labels feature)
    /// </summary>
    public ImageDto? PreviousSelectedImage { get; private set; }

    /// <summary>
    /// Set of selected image IDs (for multi-selection)
    /// </summary>
    public HashSet<int> SelectedImageIds { get; private set; } = new();

    /// <summary>
    /// Anchor image ID for shift-click range selection
    /// </summary>
    public int? AnchorImageId { get; private set; }

    /// <summary>
    /// Event fired when a single image is selected
    /// </summary>
    public event Action<ImageDto?>? OnImageSelected;

    /// <summary>
    /// Event fired when multiple images are selected
    /// </summary>
    public event Action<HashSet<int>>? OnMultipleImagesSelected;

    /// <summary>
    /// Selects a single image and tracks previous selection
    /// </summary>
    public void SelectImage(ImageDto? image)
    {
        // Update if image is different (either different ID or different object reference)
        // This is important for workspace reload scenarios where the same image ID
        // gets a new object reference with updated annotations
        if (SelectedImage == null || image == null || SelectedImage.Id != image.Id || !ReferenceEquals(SelectedImage, image))
        {
            PreviousSelectedImage = SelectedImage;
            SelectedImage = image;
            OnImageSelected?.Invoke(image);
        }
    }

    /// <summary>
    /// Selects multiple images
    /// </summary>
    public void SelectMultiple(HashSet<int> ids)
    {
        SelectedImageIds = ids;
        OnMultipleImagesSelected?.Invoke(ids);
    }

    /// <summary>
    /// Sets the anchor image for range selection
    /// </summary>
    public void SetAnchor(int? imageId)
    {
        AnchorImageId = imageId;
    }

    /// <summary>
    /// Clears all selections
    /// </summary>
    public void ClearSelection()
    {
        SelectedImageIds.Clear();
        AnchorImageId = null;
    }

    /// <summary>
    /// Adds an image ID to the selection
    /// </summary>
    public void AddToSelection(int imageId)
    {
        SelectedImageIds.Add(imageId);
        OnMultipleImagesSelected?.Invoke(SelectedImageIds);
    }

    /// <summary>
    /// Removes an image ID from the selection
    /// </summary>
    public void RemoveFromSelection(int imageId)
    {
        SelectedImageIds.Remove(imageId);
        OnMultipleImagesSelected?.Invoke(SelectedImageIds);
    }

    /// <summary>
    /// Toggles an image ID in the selection
    /// </summary>
    public void ToggleSelection(int imageId)
    {
        if (SelectedImageIds.Contains(imageId))
        {
            SelectedImageIds.Remove(imageId);
        }
        else
        {
            SelectedImageIds.Add(imageId);
        }
        OnMultipleImagesSelected?.Invoke(SelectedImageIds);
    }

    public void ClearAll()
    {
        SelectedImage = null;
        PreviousSelectedImage = null;
        SelectedImageIds.Clear();
        AnchorImageId = null;
    }

}
