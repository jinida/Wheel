using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace WheelApp.Shared
{
    public partial class LeftSideBar
    {
        private string? _currentUrl;
        private bool _isWheelCVOpen = false;
        private bool _isWheelDLOpen = false;
        private bool _isWheelADOpen = false;
        private bool _isUserProfileOpen = false;
        private bool _isBlogOpen = false;

        protected override void OnInitialized()
        {
            _currentUrl = NavigationManager.Uri;
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _isWheelCVOpen = await GetToggleState("WheelCV");
                _isWheelDLOpen = await GetToggleState("WheelDL");
                _isWheelADOpen = await GetToggleState("WheelAD");
                _isUserProfileOpen = await GetToggleState("UserProfile");
                _isBlogOpen = await GetToggleState("Blog");
                StateHasChanged();
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _currentUrl = e.Location;
            StateHasChanged();
        }

        private async Task ToggleOpen(string section)
        {
            switch (section)
            {
                case "WheelCV": _isWheelCVOpen = !_isWheelCVOpen; break;
                case "WheelDL": _isWheelDLOpen = !_isWheelDLOpen; break;
                case "WheelAD": _isWheelADOpen = !_isWheelADOpen; break;
                case "UserProfile": _isUserProfileOpen = !_isUserProfileOpen; break;
                case "Blog": _isBlogOpen = !_isBlogOpen; break;
            }
            await SetToggleState(section, GetSectionState(section));
        }

        private bool GetSectionState(string section) => section switch
        {
            "WheelCV" => _isWheelCVOpen,
            "WheelDL" => _isWheelDLOpen,
            "WheelAD" => _isWheelADOpen,
            "UserProfile" => _isUserProfileOpen,
            "Blog" => _isBlogOpen,
            _ => false
        };

        private async Task SetToggleState(string key, bool value)
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"leftbar_{key}Open", value.ToString());
        }

        private async Task<bool> GetToggleState(string key)
        {
            var value = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"leftbar_{key}Open");
            return bool.TryParse(value, out var result) && result;
        }

        private void NavigateTo(string url)
        {
            NavigationManager.NavigateTo(url);
        }

        private bool IsActive(string href)
        {
            if (_currentUrl is null) return false;
            return _currentUrl.EndsWith(href, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        [Parameter]
        public bool IsCollapsed { get; set; }

    }
}