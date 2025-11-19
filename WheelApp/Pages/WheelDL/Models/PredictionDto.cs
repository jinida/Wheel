using WheelApp.Application.DTOs;

namespace WheelApp.Pages.WheelDL.Models
{
    /// <summary>
    /// Prediction data for evaluation
    /// Contains predicted annotations based on task type
    /// </summary>
    public class PredictionDto
    {
        public int ImageId { get; set; }
        public List<PredictionAnnotationDto> Annotations { get; set; } = new();
    }

    /// <summary>
    /// Individual prediction annotation
    /// Structure matches actual annotation but represents predicted values
    /// </summary>
    public class PredictionAnnotationDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string ClassColor { get; set; } = string.Empty;

        // For Object Detection: BBox coordinates
        public Point2f[]? BBox { get; set; }

        // For Segmentation: Polygon points
        public Point2f[]? Polygon { get; set; }

        // Confidence score (0.0 ~ 1.0)
        public double Confidence { get; set; } = 1.0;
    }
}
