namespace WheelApp.Services;

/// <summary>
/// Canvas transformation service
/// Manages zoom, pan, and viewport state for ImageCanvas and MiniMap
/// Single Responsibility: Canvas transformation only
/// </summary>
public class CanvasTransformService
{
    #region Properties

    /// <summary>
    /// Current zoom level (0.25 ~ 5.0)
    /// </summary>
    public double Zoom { get; private set; } = 1.0;

    /// <summary>
    /// X-axis pan offset in pixels
    /// </summary>
    public double PanX { get; private set; } = 0;

    /// <summary>
    /// Y-axis pan offset in pixels
    /// </summary>
    public double PanY { get; private set; } = 0;

    /// <summary>
    /// Base scale to fit image to viewport
    /// Calculated as: Math.Min(viewportWidth / imageWidth, viewportHeight / imageHeight)
    /// </summary>
    public double BaseScale { get; private set; } = 0;

    /// <summary>
    /// Loaded image width in pixels
    /// </summary>
    public int ImageWidth { get; private set; } = 0;

    /// <summary>
    /// Loaded image height in pixels
    /// </summary>
    public int ImageHeight { get; private set; } = 0;

    /// <summary>
    /// Viewport (canvas container) width
    /// </summary>
    public double ViewportWidth { get; private set; } = 0;

    /// <summary>
    /// Viewport (canvas container) height
    /// </summary>
    public double ViewportHeight { get; private set; } = 0;

    /// <summary>
    /// Whether currently panning
    /// </summary>
    public bool IsPanning { get; private set; } = false;

    /// <summary>
    /// Last mouse X coordinate for pan operation
    /// </summary>
    public double LastMouseX { get; private set; } = 0;

    /// <summary>
    /// Last mouse Y coordinate for pan operation
    /// </summary>
    public double LastMouseY { get; private set; } = 0;

    /// <summary>
    /// Whether transform calculation is ready
    /// </summary>
    public bool IsTransformReady { get; private set; } = false;

    #endregion

    #region Events

    /// <summary>
    /// Event fired when zoom level changes
    /// </summary>
    public event Action<double>? OnZoomChanged;

    /// <summary>
    /// Event fired when pan position changes
    /// </summary>
    public event Action<double, double>? OnPanChanged;

    /// <summary>
    /// Event fired when transform calculation is complete
    /// </summary>
    public event Action? OnTransformReady;

    #endregion

    #region Methods

    /// <summary>
    /// Sets the zoom level (clamped to 0.25~5.0 range)
    /// </summary>
    /// <param name="zoom">Target zoom level</param>
    public void SetZoom(double zoom)
    {
        var newZoom = Math.Max(0.25, Math.Min(5.0, zoom));
        if (Math.Abs(Zoom - newZoom) < 0.001) return; // No significant change

        Zoom = newZoom;
        OnZoomChanged?.Invoke(Zoom);
    }

    /// <summary>
    /// Sets the pan position
    /// </summary>
    /// <param name="panX">X-axis pan offset</param>
    /// <param name="panY">Y-axis pan offset</param>
    public void SetPan(double panX, double panY)
    {
        PanX = panX;
        PanY = panY;
        OnPanChanged?.Invoke(PanX, PanY);
    }

    /// <summary>
    /// Sets the image dimensions and triggers BaseScale recalculation
    /// </summary>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    public void SetImageDimensions(int width, int height)
    {
        ImageWidth = width;
        ImageHeight = height;

        // Recalculate base scale if viewport is already set
        if (ViewportWidth > 0 && ViewportHeight > 0)
        {
            CalculateBaseScale();
        }
    }

    /// <summary>
    /// Sets the viewport dimensions and triggers BaseScale recalculation
    /// </summary>
    /// <param name="width">Viewport width</param>
    /// <param name="height">Viewport height</param>
    public void SetViewportDimensions(double width, double height)
    {
        ViewportWidth = width;
        ViewportHeight = height;

        // Recalculate base scale if image is already loaded
        if (ImageWidth > 0 && ImageHeight > 0)
        {
            CalculateBaseScale();
        }
    }

    /// <summary>
    /// Calculates the base scale to fit image in viewport
    /// Formula: Math.Min(viewportWidth / imageWidth, viewportHeight / imageHeight)
    /// </summary>
    public void CalculateBaseScale()
    {
        if (ImageWidth <= 0 || ImageHeight <= 0 || ViewportWidth <= 0 || ViewportHeight <= 0)
        {
            BaseScale = 1.0;
            IsTransformReady = false;
            return;
        }

        // Calculate scale to fit entire image in viewport
        var scaleX = ViewportWidth / ImageWidth;
        var scaleY = ViewportHeight / ImageHeight;

        // Use the smaller scale to ensure entire image fits
        BaseScale = Math.Min(scaleX, scaleY);

        // Center the image in the viewport
        var scaledWidth = ImageWidth * BaseScale * Zoom;
        var scaledHeight = ImageHeight * BaseScale * Zoom;

        PanX = (ViewportWidth - scaledWidth) / 2;
        PanY = (ViewportHeight - scaledHeight) / 2;

        IsTransformReady = true;
        OnTransformReady?.Invoke();
        OnPanChanged?.Invoke(PanX, PanY);
    }

    /// <summary>
    /// Resets zoom to 1.0 and recenters the image
    /// </summary>
    public void ResetZoom()
    {
        Zoom = 1.0;
        OnZoomChanged?.Invoke(Zoom);

        // Recenter the image at zoom=1.0
        if (ImageWidth > 0 && ImageHeight > 0 && ViewportWidth > 0 && ViewportHeight > 0)
        {
            var scaledWidth = ImageWidth * BaseScale;
            var scaledHeight = ImageHeight * BaseScale;

            PanX = (ViewportWidth - scaledWidth) / 2;
            PanY = (ViewportHeight - scaledHeight) / 2;

            OnPanChanged?.Invoke(PanX, PanY);
        }
        else
        {
            PanX = 0;
            PanY = 0;
            OnPanChanged?.Invoke(PanX, PanY);
        }
    }

    /// <summary>
    /// Resets all transform state to initial values
    /// </summary>
    public void ResetAll()
    {
        Zoom = 1.0;
        PanX = 0;
        PanY = 0;
        BaseScale = 0;
        ImageWidth = 0;
        ImageHeight = 0;
        ViewportWidth = 0;
        ViewportHeight = 0;
        IsPanning = false;
        LastMouseX = 0;
        LastMouseY = 0;
        IsTransformReady = false;

        OnZoomChanged?.Invoke(Zoom);
        OnPanChanged?.Invoke(PanX, PanY);
    }

    /// <summary>
    /// Gets the total scale (BaseScale * Zoom)
    /// </summary>
    /// <returns>Total scale factor</returns>
    public double GetTotalScale()
    {
        return BaseScale * Zoom;
    }

    /// <summary>
    /// Starts a pan operation
    /// </summary>
    /// <param name="mouseX">Starting mouse X coordinate</param>
    /// <param name="mouseY">Starting mouse Y coordinate</param>
    public void StartPan(double mouseX, double mouseY)
    {
        IsPanning = true;
        LastMouseX = mouseX;
        LastMouseY = mouseY;
    }

    /// <summary>
    /// Ends the pan operation
    /// </summary>
    public void EndPan()
    {
        IsPanning = false;
    }

    #endregion
}
