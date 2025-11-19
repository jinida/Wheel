using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Services;
using WheelApp.Infrastructure.Identity;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Interceptors;
using WheelApp.Infrastructure.Persistence.Repositories;
using WheelApp.Infrastructure.Services;
using WheelApp.Infrastructure.Storage;

namespace WheelApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Interceptors are Scoped to match DbContext lifetime
            services.AddScoped<AuditInterceptor>();
            services.AddScoped<DomainEventInterceptor>();

            // DbContext registered as Scoped (single instance per request)
            services.AddDbContext<WheelAppDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                // Enable Multiple Active Result Sets (MARS) to allow concurrent queries
                // This solves the "second operation started" error in Blazor Server
                var connStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                {
                    MultipleActiveResultSets = true
                };

                options.UseSqlServer(connStringBuilder.ConnectionString);

                // Add interceptors for audit and domain events
                options.AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventInterceptor>());

                // Configure warnings
                options.ConfigureWarnings(warnings =>
                {
                    // Suppress MARS savepoint warning - we handle rollback manually in UnitOfWork
                    warnings.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS);

                    // Suppress multiple collection include warning - our queries are already optimized
                    warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
                });
            });

            // UnitOfWork wraps the Scoped DbContext
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IDatasetRepository, DatasetRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IImageRepository, ImageRepository>();
            services.AddScoped<IAnnotationRepository, AnnotationRepository>();
            services.AddScoped<IProjectClassRepository, ProjectClassRepository>();
            services.AddScoped<ITrainingRepository, TrainingRepository>();
            services.AddScoped<IEvaluationRepository, EvaluationRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();

            // Storage
            services.Configure<StorageOptions>(options => configuration.GetSection("Storage").Bind(options));
            services.AddSingleton<IFileStorage, LocalFileStorage>();

            // Identity
            services.TryAddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Common Services
            services.AddSingleton<IDateTime, DateTimeService>();

            // Domain Services
            services.AddScoped<IRoleSplitService, RoleSplitService>();

            // Training Services
            services.AddScoped<ITrainingProgressCalculator, TrainingProgressCalculator>();

            return services;
        }
    }
}