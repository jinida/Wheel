using Microsoft.AspNetCore.Components;

namespace WheelApp.Components
{
    public partial class BarGraph : ComponentBase
    {
        [Parameter]
        public string Title { get; set; } = "";

        [Parameter]
        public Dictionary<string, int> Data { get; set; } = new();

        [Parameter]
        public bool UseRandomColors { get; set; } = true;

        [Parameter]
        public string BarColor { get; set; } = "#007bff";

        [Parameter]
        public List<string>? BarColors { get; set; }

        [Parameter]
        public string ValueUnit { get; set; } = "";

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

        private List<(string Text, double PositionPercent)> _yAxisData = new();
        private List<(string FormattedValue, string HeightPercent, string Color)> _processedBars = new();
        private double _niceMaxValue = 100;
        private bool _isPercentageMode = false;

        protected override void OnParametersSet()
        {
            if (Data == null || Data.Count == 0)
            {
                _yAxisData.Clear();
                _processedBars.Clear();
                return;
            }
            CalculateYAxis();
            ProcessBarData();
        }

        private void TogglePercentageMode()
        {
            _isPercentageMode = !_isPercentageMode;
            CalculateYAxis();
            ProcessBarData();
        }

        private void CalculateYAxis()
        {
            _yAxisData.Clear();
            const int labelCount = 5;
            if (_isPercentageMode) { _niceMaxValue = 100; }
            else
            {
                double maxValue = Data.Values.Any() ? Data.Values.Max() : 0;
                if (maxValue <= 0) { _niceMaxValue = 10; }
                else if (maxValue <= 10) { _niceMaxValue = 10; }
                else
                {
                    double divisor;
                    if (maxValue > 100) { divisor = Math.Pow(10, Math.Floor(Math.Log10(maxValue)) - 1); }
                    else { divisor = Math.Pow(10, Math.Floor(Math.Log10(maxValue))); }
                    _niceMaxValue = Math.Ceiling(maxValue / divisor) * divisor;
                }
            }
            for (int i = 0; i < labelCount; i++)
            {
                double stepValue = _niceMaxValue * (1 - (double)i / (labelCount - 1));
                double position = (double)i / (labelCount - 1) * 100;
                string labelText = _isPercentageMode ? $"{stepValue:N0}%" : $"{stepValue.ToString("N0")}{ValueUnit}";
                _yAxisData.Add((labelText, position));
            }
        }

        private void ProcessBarData()
        {
            _processedBars.Clear();
            int index = 0;
            if (_isPercentageMode)
            {
                double totalSum = Data.Values.Sum();
                if (totalSum == 0) return;
                foreach (var item in Data)
                {
                    double value = item.Value;
                    double percentage = (value / totalSum) * 100;
                    string color = GetColorForBar(index);
                    string formattedValue = $"{value.ToString("N0")} ({percentage:F1}%)";
                    _processedBars.Add((formattedValue, percentage.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), color));
                    index++;
                }
            }
            else
            {
                foreach (var item in Data)
                {
                    double value = item.Value;
                    double heightPercent = (_niceMaxValue == 0) ? 0 : (value / _niceMaxValue) * 100;
                    string color = GetColorForBar(index);
                    string formattedValue = $"{value.ToString("N0")}{ValueUnit}";
                    _processedBars.Add((formattedValue, heightPercent.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), color));
                    index++;
                }
            }
        }

        private string GetColorForBar(int index)
        {
            if (BarColors != null && BarColors.Count > index)
            {
                return BarColors[index];
            }
            if (UseRandomColors)
            {
                return GetRandomColor();
            }
            return BarColor;
        }

        private string GetRandomColor() => $"#{Random.Shared.Next(0x1000000):X6}";
    }
}
