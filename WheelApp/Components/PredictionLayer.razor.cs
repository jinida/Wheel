using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using WheelApp.Application.DTOs;
using WheelApp.Pages.WheelDL.Models;

namespace WheelApp.Components
{
    public partial class PredictionLayer : ComponentBase
    {
        [Parameter] public List<PredictionAnnotationDto>? Predictions { get; set; }
        [Parameter] public int? ProjectType { get; set; }
        [Parameter] public double Zoom { get; set; } = 1.0;

        private RenderFragment RenderBBoxPrediction(PredictionAnnotationDto prediction, double zoom) => (RenderTreeBuilder builder) =>
        {
            var bbox = prediction.BBox!;
            var x = Math.Min(bbox[0].X, bbox[2].X);
            var y = Math.Min(bbox[0].Y, bbox[2].Y);
            var width = Math.Abs(bbox[2].X - bbox[0].X);
            var height = Math.Abs(bbox[2].Y - bbox[0].Y);
            var strokeWidth = 2 / zoom;
            var dashArray = $"{5 / zoom},{3 / zoom}";
            var fontSize = 12 / zoom;
            var textY = y - 5 / zoom;
            var label = $"{System.Net.WebUtility.HtmlEncode(prediction.ClassName)} ({prediction.Confidence:P0})";

            // Render filled rectangle with semi-transparent fill
            builder.OpenElement(0, "rect");
            builder.AddAttribute(1, "x", x);
            builder.AddAttribute(2, "y", y);
            builder.AddAttribute(3, "width", width);
            builder.AddAttribute(4, "height", height);
            builder.AddAttribute(5, "fill", prediction.ClassColor);
            builder.AddAttribute(6, "fill-opacity", "0.2");
            builder.AddAttribute(7, "stroke", prediction.ClassColor);
            builder.AddAttribute(8, "stroke-width", strokeWidth);
            builder.AddAttribute(9, "stroke-dasharray", dashArray);
            builder.AddAttribute(10, "opacity", "0.8");
            builder.AddAttribute(11, "pointer-events", "none");
            builder.CloseElement();

            builder.AddMarkupContent(12, $"<text x=\"{x}\" y=\"{textY}\" font-size=\"{fontSize}\" fill=\"{prediction.ClassColor}\" font-weight=\"bold\" opacity=\"0.9\" pointer-events=\"none\">{label}</text>");
        };

        private RenderFragment RenderPolygonPrediction(PredictionAnnotationDto prediction, double zoom) => (RenderTreeBuilder builder) =>
        {
            var points = string.Join(" ", prediction.Polygon!.Select(p => $"{p.X},{p.Y}"));
            var centerX = prediction.Polygon!.Average(p => p.X);
            var centerY = prediction.Polygon!.Average(p => p.Y);
            var strokeWidth = 2 / zoom;
            var dashArray = $"{5 / zoom},{3 / zoom}";
            var fontSize = 12 / zoom;
            var label = $"{System.Net.WebUtility.HtmlEncode(prediction.ClassName)} ({prediction.Confidence:P0})";

            builder.OpenElement(0, "polygon");
            builder.AddAttribute(1, "points", points);
            builder.AddAttribute(2, "fill", prediction.ClassColor);
            builder.AddAttribute(3, "fill-opacity", "0.2");
            builder.AddAttribute(4, "stroke", prediction.ClassColor);
            builder.AddAttribute(5, "stroke-width", strokeWidth);
            builder.AddAttribute(6, "stroke-dasharray", dashArray);
            builder.AddAttribute(7, "opacity", "0.8");
            builder.AddAttribute(8, "pointer-events", "none");
            builder.CloseElement();

            builder.AddMarkupContent(9, $"<text x=\"{centerX}\" y=\"{centerY}\" font-size=\"{fontSize}\" fill=\"{prediction.ClassColor}\" font-weight=\"bold\" text-anchor=\"middle\" opacity=\"0.9\" pointer-events=\"none\">{label}</text>");
        };
    }
}
