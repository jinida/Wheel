using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Trainings.Commands.CreateTraining;

/// <summary>
/// Handles the creation of a new training with Pending status
/// </summary>
public class CreateTrainingCommandHandler : ICommandHandler<CreateTrainingCommand, Result<TrainingDto>>
{
    private readonly ITrainingRepository _trainingRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateTrainingCommandHandler> _logger;

    public CreateTrainingCommandHandler(
        ITrainingRepository trainingRepository,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateTrainingCommandHandler> logger)
    {
        _trainingRepository = trainingRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<TrainingDto>> Handle(CreateTrainingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating training '{TrainingName}' for project {ProjectId}",
            request.Name, request.ProjectId);

        // Validate project exists
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
        {
            _logger.LogWarning("Failed to create training. Project with ID {ProjectId} not found", request.ProjectId);
            return Result.Failure<TrainingDto>($"Project with ID {request.ProjectId} was not found.");
        }

        // Create domain entity with Pending status
        var training = Training.Start(request.ProjectId, request.Name);

        // Persist to repository
        await _trainingRepository.AddAsync(training, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Successfully created training {TrainingId} with name '{TrainingName}' and status Pending",
            training.Id, request.Name);

        // Map to DTO and return (Progress will be 0 for Pending status)
        var trainingDto = _mapper.Map<TrainingDto>(training);
        trainingDto.Progress = 0; // Pending status always has 0 progress

        return Result.Success(trainingDto);
    }
}
