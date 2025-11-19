using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Trainings.Queries.GetActiveTrainings;

/// <summary>
/// Query to get all active trainings (Pending or Running)
/// </summary>
public record GetActiveTrainingsQuery : IQuery<Result<List<TrainingDto>>>;
