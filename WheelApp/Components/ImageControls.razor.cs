using Microsoft.AspNetCore.Components;

namespace WheelApp.Components
{
    public partial class ImageControls
    {
        public enum ControlPosition { Top, Bottom }

        [Parameter]
        public ControlPosition Position { get; set; } = ControlPosition.Top;

        [Parameter]
        public double Zoom { get; set; } = 1.0;

        [Parameter]
        public EventCallback OnZoomIn { get; set; }

        [Parameter]
        public EventCallback OnZoomOut { get; set; }

        [Parameter]
        public EventCallback<double> OnZoomChanged { get; set; }

        [Parameter]
        public EventCallback OnResetZoom { get; set; }

        [Parameter]
        public double Brightness { get; set; } = 100;

        [Parameter]
        public EventCallback<double> OnBrightnessChanged { get; set; }

        [Parameter]
        public double Gamma { get; set; } = 1.0;

        [Parameter]
        public EventCallback<double> OnGammaChanged { get; set; }

        [Parameter]
        public double Contrast { get; set; } = 100;

        [Parameter]
        public EventCallback<double> OnContrastChanged { get; set; }

        [Parameter]
        public EventCallback OnResetAdjustments { get; set; }
    }
}