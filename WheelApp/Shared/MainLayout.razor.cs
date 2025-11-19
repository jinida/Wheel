using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace WheelApp.Shared
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        private bool _isLeftSidebarCollapsed = false;
        private bool _isRightSidebarCollapsed = false;
        private void ToggleLeftSidebar()
        {
            _isLeftSidebarCollapsed = !_isLeftSidebarCollapsed;
        }

        private void ToggleRightSidebar()
        {
            _isRightSidebarCollapsed = !_isRightSidebarCollapsed;
        }

        [Inject]
        private NavigationManager _navigationManager { get; set; } = default!;
        private List<string> _breadcrumbs = new List<string>();

        protected override void OnInitialized()
        {
            UpdateBreadcrumbs(_navigationManager.Uri);
            _navigationManager.LocationChanged += OnLocationChanged;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            UpdateBreadcrumbs(e.Location);
            StateHasChanged();
        }

        private void UpdateBreadcrumbs(string uri)
        {
            _breadcrumbs.Clear();

            var segments = new Uri(uri).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                var breadcrumbText = segment switch
                {
                    "introduce" => "Introduce",
                    "wheelcv" => "WheelCV",
                    "wheeldl" => "WheelDL",
                    "wheelad" => "WheelAD",
                    "user" => "User Profile",
                    "blog" => "Blog",
                    "papers" => "Papers",
                    _ => char.ToUpper(segment[0]) + segment.Substring(1)
                };
                _breadcrumbs.Add(breadcrumbText);
            }
        }

        public void Dispose()
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}