using Microsoft.AspNetCore.Components;

namespace WheelApp.Components
{
    /// <summary>
    /// CoordinateDisplay Component - Shows mouse coordinates on the image
    /// </summary>
    public partial class CoordinateDisplay
    {
        [Parameter] public bool Show { get; set; }
        [Parameter] public int? MouseX { get; set; }
        [Parameter] public int? MouseY { get; set; }
    }
}
