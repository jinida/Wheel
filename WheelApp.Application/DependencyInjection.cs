using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WheelApp.Application.Common.Behaviors;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Options;
using WheelApp.Application.Common.Services;
using WheelApp.Application.Services;

namespace WheelApp.Application;

/// <summary>
/// Dependency Injection configuration for Application layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Application services with the DI container
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MediatR with pipeline behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            // Add pipeline behaviors in order
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // Register AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure options
        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));

        // Register common services
        services.AddScoped<ImageValidationService>();

        // Register concurrency control (Scoped per user session/SignalR connection)
        services.AddScoped<DbContextConcurrencyGuard>();

        return services;
    }
}
