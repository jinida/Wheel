using WheelApp.Pages.WheelDL.Coordinators;

namespace WheelApp.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection
    /// Provides clean registration of application services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Phase 2 Coordinators that require DI
        /// Coordinators with heavy dependencies (IMediator, Phase 1 services) are registered here
        /// Lightweight helpers (KeyboardListenerHelper, UIEnhancementHelper, GridSortHelper)
        /// are instantiated directly in components without DI registration
        /// </summary>
        public static IServiceCollection AddProjectCoordinators(this IServiceCollection services)
        {
            // Phase 2 Coordinators - Require DI (8 coordinators)
            services.AddScoped<ProjectWorkspaceCoordinator>();
            services.AddScoped<ProjectClassManagementCoordinator>();
            services.AddScoped<ProjectAnnotationCoordinator>();
            services.AddScoped<ProjectRoleCoordinator>();
            services.AddScoped<ProjectFileCoordinator>();
            services.AddScoped<ProjectImageSelectionCoordinator>();
            services.AddScoped<RightSideBarCoordinator>();
            services.AddScoped<TrainingCoordinator>();

            // Note: KeyboardListenerHelper, UIEnhancementHelper, GridSortHelper
            // are NOT registered here - they are instantiated directly with 'new'

            return services;
        }
    }
}
