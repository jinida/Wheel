namespace WheelApp.Components
{
    public partial class Toast
    {
        private List<ToastItem> _toasts = new();
        private readonly object _toastLock = new();

        private List<ToastItem> GetToastsCopy()
        {
            lock (_toastLock)
            {
                return _toasts.ToList();
            }
        }

        public void Show(string title, string? message = null, string type = "info", int duration = 3000)
        {
            var toast = new ToastItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Message = message,
                Type = type,
                IsVisible = false
            };

            lock (_toastLock)
            {
                _toasts.Add(toast);
            }
            StateHasChanged();

            // Trigger animation
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(10);
                    toast.IsVisible = true;
                    await InvokeAsync(StateHasChanged);

                    await Task.Delay(duration);

                    toast.IsVisible = false;
                    await InvokeAsync(StateHasChanged);

                    await Task.Delay(300); // Wait for fade out animation

                    await InvokeAsync(() =>
                    {
                        lock (_toastLock)
                        {
                            _toasts.Remove(toast);
                        }
                        StateHasChanged();
                    });
                }
                catch (Exception ex)
                {
                    // Log the error but don't crash the app
                    Console.WriteLine($"Toast animation error: {ex.Message}");
                }
            });
        }

        private class ToastItem
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = "";
            public string? Message { get; set; }
            public string Type { get; set; } = "info";
            public bool IsVisible { get; set; }
        }
    }
}