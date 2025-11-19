using WheelApp.Application.DTOs;

namespace WheelApp.Components
{
    /// <summary>
    /// ImageCanvas - Helper methods
    /// Utility methods for geometric calculations and validation
    /// </summary>
    public partial class ImageCanvas
    {
        /// <summary>
        /// Static helper methods for geometric calculations
        /// Accessible from child components as ImageCanvas.Helpers.*
        /// </summary>
        public static class Helpers
        {
            /// <summary>
            /// Gets normalized bounding box coordinates from two points.
            /// Returns (x1, y1, x2, y2) where (x1, y1) is top-left and (x2, y2) is bottom-right.
            /// </summary>
            public static (float x1, float y1, float x2, float y2) GetBboxCoordinates(Point2f p0, Point2f p1)
            {
                var x1 = Math.Min(p0.X, p1.X);
                var y1 = Math.Min(p0.Y, p1.Y);
                var x2 = Math.Max(p0.X, p1.X);
                var y2 = Math.Max(p0.Y, p1.Y);
                return (x1, y1, x2, y2);
            }

            /// <summary>
            /// Gets bounding box rendering bounds from two points.
            /// Returns (x, y, width, height) for use in SVG rendering.
            /// </summary>
            public static (float x, float y, float width, float height) GetBboxBounds(Point2f p0, Point2f p1)
            {
                var (x1, y1, x2, y2) = GetBboxCoordinates(p0, p1);
                return (x1, y1, x2 - x1, y2 - y1);
            }

            /// <summary>
            /// Calculates Euclidean distance between two points.
            /// </summary>
            public static double CalculateDistance(double x1, double y1, double x2, double y2)
            {
                return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            }
        }

        private Point2f ClampPointToImage(double x, double y)
        {
            return new Point2f((float)Math.Max(0, Math.Min(_canvasTransformService.ImageWidth, x)), (float)Math.Max(0, Math.Min(_canvasTransformService.ImageHeight, y)));
        }

        private void ClampAllAnnotationsToImage()
        {
            if (_canvasTransformService.ImageWidth <= 0 || _canvasTransformService.ImageHeight <= 0) return;

            foreach (var annotation in _annotations)
            {
                for (int i = 0; i < annotation.Information.Count; i++)
                {
                    annotation.Information[i] = ClampPointToImage(annotation.Information[i].X, annotation.Information[i].Y);
                }
            }
        }


        /// <summary>
        /// Checks if annotation shortcuts should be disabled for current task type.
        /// Returns true if shortcuts (z, x, c, r, Ctrl+C, Ctrl+V) should be disabled.
        /// </summary>
        private bool AreAnnotationShortcutsDisabled()
        {
            // Disable annotation shortcuts for Classification and AnomalyDetection
            return !_projectWorkspaceService.CurrentProjectType.MultiLabelType;
        }

        /// <summary>
        /// Toggles prediction visibility in evaluation mode
        /// </summary>
        private void TogglePredictions()
        {
            _showPredictions = !_showPredictions;
        }

        /// <summary>
        /// Toggles label visibility in evaluation mode
        /// </summary>
        private void ToggleLabels()
        {
            _showLabels = !_showLabels;
        }
    }
}
