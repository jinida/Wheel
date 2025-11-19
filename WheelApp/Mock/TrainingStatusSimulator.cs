using MediatR;
using WheelApp.Application.UseCases.Trainings.Queries.GetActiveTrainings;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Mock;

/// <summary>
/// Mock service to simulate training status transitions for testing
/// Automatically transitions trainings: Pending -> Running -> Completed
/// </summary>
public class TrainingStatusSimulator : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrainingStatusSimulator> _logger;
    private Timer? _timer;
    private bool _isEnabled;

    public TrainingStatusSimulator(
        IServiceProvider serviceProvider,
        ILogger<TrainingStatusSimulator> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("MockServices:TrainingSimulator:Enabled");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Training Status Simulator is disabled");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Training Status Simulator started");

        // Run every 10 seconds
        _timer = new Timer(SimulateStatusTransitions, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

        return Task.CompletedTask;
    }

    private async void SimulateStatusTransitions(object? state)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var trainingRepository = scope.ServiceProvider.GetRequiredService<ITrainingRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IUnitOfWork>();

            var allTrainings = await trainingRepository.GetAllAsync();

            foreach (var training in allTrainings)
            {
                var elapsed = DateTime.UtcNow - training.CreatedAt;

                // Pending -> Running after 5 seconds
                if (training.Status.Value == TrainingStatus.Pending.Value && elapsed.TotalSeconds >= 5)
                {
                    // Detach all tracked entities to avoid tracking conflicts
                    trainingRepository.Detach(training);

                    training.UpdateStatus(TrainingStatus.Running.Value);
                    await trainingRepository.UpdateAsync(training);
                    _logger.LogInformation("Training {TrainingId} '{TrainingName}' transitioned to Running",
                        training.Id, training.Name.Value);
                }
                // Running -> Completed after 5 minutes (300 seconds)
                else if (training.Status.Value == TrainingStatus.Running.Value && elapsed.TotalMinutes >= 5)
                {
                    // Detach all tracked entities to avoid tracking conflicts
                    trainingRepository.Detach(training);

                    training.UpdateStatus(TrainingStatus.Completed.Value);
                    await trainingRepository.UpdateAsync(training);
                    _logger.LogInformation("Training {TrainingId} '{TrainingName}' completed",
                        training.Id, training.Name.Value);
                }
            }

            await unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Training Status Simulator");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Training Status Simulator stopped");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
