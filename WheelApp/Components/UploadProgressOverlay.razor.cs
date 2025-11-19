using Microsoft.AspNetCore.Components;

namespace WheelApp.Components
{
    public partial class UploadProgressOverlay
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public string ProgressMessage { get; set; } = "Uploading...";
        [Parameter] public int UploadedFiles { get; set; }
        [Parameter] public int TotalFiles { get; set; }

        private int ProgressPercentage => TotalFiles > 0 ? (int)((double)UploadedFiles / TotalFiles * 100) : 0;
        private double Circumference => 2 * Math.PI * 52; // radius = 52
        private string CircumferencePx => $"{Circumference}px";
        private string OffsetPx => $"{Circumference - (ProgressPercentage / 100.0 * Circumference)}px";
    }
}