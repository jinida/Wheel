using System.Net.Http;
using Serilog;
using WheelApp.Application;
using WheelApp.Infrastructure;
using WheelApp.Services;
using WheelApp.Extensions;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Configure Kestrel for large file uploads
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.MaxRequestBodySize = 1024 * 1024 * 1024; // 1 GB
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
        });

        // Add Blazor Server services
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor(options =>
        {
            options.DetailedErrors = true;
            options.DisconnectedCircuitMaxRetained = 100;
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
            options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(5);
        })
        .AddHubOptions(options =>
        {
            options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
            options.HandshakeTimeout = TimeSpan.FromMinutes(1);
            options.MaximumReceiveMessageSize = 512 * 1024 * 1024; // 512 MB
        });

        builder.Services.AddScoped(sp =>
        {
            return new HttpClient { BaseAddress = new Uri("https://localhost:7150"), Timeout = TimeSpan.FromMinutes(10) };
        });
        builder.Services.AddControllers();

        // Configure form options for large file uploads
        builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 1024 * 1024 * 1024; // 1 GB
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });

        // Add clean architecture layers
        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        // Phase 1: Domain-specific services
        builder.Services.AddScoped<ProjectWorkspaceService>();
        builder.Services.AddScoped<ImageSelectionService>();
        builder.Services.AddScoped<AnnotationService>();
        builder.Services.AddScoped<DrawingToolService>();
        builder.Services.AddScoped<ClassManagementService>();
        builder.Services.AddScoped<CanvasTransformService>(); // Canvas zoom/pan state (shared with MiniMap)

        // Phase 2: Coordinators (orchestrate services for UI workflows)
        builder.Services.AddProjectCoordinators();

        // Add ImageCanvas services (from backup - for canvas annotation features)

        // Mock Services for Testing (only if enabled in configuration)
        if (builder.Configuration.GetValue<bool>("MockServices:TrainingSimulator:Enabled"))
        {
            builder.Services.AddHostedService<WheelApp.Mock.TrainingStatusSimulator>();
        }

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.MapControllers();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}