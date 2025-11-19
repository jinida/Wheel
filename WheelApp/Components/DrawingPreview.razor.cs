using Microsoft.AspNetCore.Components;
using WheelApp.Application.DTOs;

namespace WheelApp.Components
{
    /// <summary>
    /// DrawingPreview Component - Shows preview during drawing operations
    /// </summary>
    public partial class DrawingPreview
    {
        [Parameter] public AnnotationDto? CurrentDrawing { get; set; }
        [Parameter] public Point2f? CurrentMousePosition { get; set; }
        [Parameter] public bool IsNearFirstPoint { get; set; }
        [Parameter] public string? SelectedColor { get; set; }
        [Parameter] public int? ProjectType { get; set; }
        [Parameter] public double Zoom { get; set; }
        [Parameter] public bool IsDraggingSelection { get; set; }
        [Parameter] public Point2f? SelectionStartPoint { get; set; }
        [Parameter] public Point2f? SelectionEndPoint { get; set; }
    }
}
