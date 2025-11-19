using Microsoft.AspNetCore.Components;
using System.Text;

namespace WheelApp.Components
{
    public partial class SnowButton : ComponentBase
    {
        public enum Variant { Filled, Light, Outlined, Plain }
        public enum Size { Small, Medium, Large }

        [Parameter] public Variant ButtonVariant { get; set; }
        [Parameter] public Size ButtonSize { get; set; }
        [Parameter] public bool Disabled { get; set; } = false;
        [Parameter] public string? LeadingIconUrl { get; set; }
        [Parameter] public string? TrailingIconUrl { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public EventCallback OnClick { get; set; }
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        private string _cssClass = "";

        protected override void OnParametersSet()
        {
            var builder = new StringBuilder("snow-button");
            builder.Append($" size-{ButtonSize.ToString().ToLower()}");
            builder.Append($" variant-{ButtonVariant.ToString().ToLower()}");

            if (ChildContent == null && (LeadingIconUrl != null || TrailingIconUrl != null))
            {
                builder.Append(" icon-only");
            }

            _cssClass = builder.ToString();
        }
    }
}
