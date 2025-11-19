using Microsoft.AspNetCore.Components;
using WheelApp.Application.DTOs;
using WheelApp.Service;
using WheelApp.Services;

namespace WheelApp.Components
{
    /// <summary>
    /// AnnotationLayer Component - Renders all existing annotations (BBox and Polygon)
    /// </summary>
    public partial class AnnotationLayer
    {
        [Parameter] public List<AnnotationDto> Annotations { get; set; } = new();
        [Parameter] public HashSet<int> SelectedAnnotationIds { get; set; } = new();
        [Parameter] public int? ProjectType { get; set; }
        [Parameter] public LabelMode CurrentMode { get; set; }
        [Parameter] public double Zoom { get; set; } = 1.0;
        [Parameter] public EventCallback<(int annotationId, Microsoft.AspNetCore.Components.Web.MouseEventArgs e)> OnAnnotationMouseDown { get; set; }
        [Parameter] public EventCallback<(int annotationId, ResizeHandle handle, Microsoft.AspNetCore.Components.Web.MouseEventArgs e)> OnStartResizeBBox { get; set; }
        [Parameter] public EventCallback<(int annotationId, int pointIndex, Microsoft.AspNetCore.Components.Web.MouseEventArgs e)> OnStartMovePolygonPoint { get; set; }
    }
}
