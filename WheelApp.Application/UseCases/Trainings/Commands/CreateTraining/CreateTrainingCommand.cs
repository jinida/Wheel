using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Trainings.Commands.CreateTraining;

/// <summary>
/// Command to create a new training with Pending status
/// </summary>
public record CreateTrainingCommand : ICommand<Result<TrainingDto>>
{
    public int ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
}
