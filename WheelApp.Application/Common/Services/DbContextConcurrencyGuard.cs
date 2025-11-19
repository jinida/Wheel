namespace WheelApp.Application.Common.Services
{
    /// <summary>
    /// Scoped service that ensures only one database operation runs at a time per user session.
    /// Prevents "A second operation was started on this context instance" errors in Blazor Server
    /// when users rapidly click buttons that trigger multiple concurrent commands.
    /// </summary>
    public class DbContextConcurrencyGuard
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SemaphoreSlim Semaphore => _semaphore;
    }
}
