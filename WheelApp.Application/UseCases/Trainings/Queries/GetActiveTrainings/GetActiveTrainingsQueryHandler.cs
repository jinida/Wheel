using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.Trainings.Queries.GetActiveTrainings;

/// <summary>
/// Handles retrieval of active trainings with dynamically calculated progress
/// </summary>
public class GetActiveTrainingsQueryHandler : IQueryHandler<GetActiveTrainingsQuery, Result<List<TrainingDto>>>
{
    private readonly ITrainingRepository _trainingRepository;
    private readonly IMapper _mapper;
    private readonly ITrainingProgressCalculator _progressCalculator;
    private readonly ILogger<GetActiveTrainingsQueryHandler> _logger;

    public GetActiveTrainingsQueryHandler(
        ITrainingRepository trainingRepository,
        IMapper mapper,
        ITrainingProgressCalculator progressCalculator,
        ILogger<GetActiveTrainingsQueryHandler> logger)
    {
        _trainingRepository = trainingRepository;
        _mapper = mapper;
        _progressCalculator = progressCalculator;
        _logger = logger;
    }

    public async Task<Result<List<TrainingDto>>> Handle(GetActiveTrainingsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching active trainings");

        // Get all trainings with Pending or Running status
        var allTrainings = await _trainingRepository.GetAllAsync(cancellationToken);
        var activeTrainings = allTrainings
            .Where(t => t.Status.Value == TrainingStatus.Pending.Value ||
                       t.Status.Value == TrainingStatus.Running.Value)
            .ToList();

        // Map to DTOs and calculate progress dynamically
        var trainingDtos = activeTrainings.Select(training =>
        {
            var dto = _mapper.Map<TrainingDto>(training);
            dto.Progress = _progressCalculator.CalculateProgress(training);
            return dto;
        }).ToList();

        _logger.LogInformation("Found {Count} active trainings", trainingDtos.Count);

        return Result.Success(trainingDtos);
    }
}
