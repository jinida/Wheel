using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Exceptions;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.ValueObjects;
using WheelApp.Domain.Specifications.ImageSpecifications;

namespace WheelApp.Application.UseCases.Projects.Commands.CreateProject;

/// <summary>
/// Handles the creation of a new project
/// </summary>
public class CreateProjectCommandHandler : ICommandHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProjectCommandHandler> _logger;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        IDatasetRepository datasetRepository,
        IImageRepository imageRepository,
        IProjectClassRepository projectClassRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _datasetRepository = datasetRepository;
        _imageRepository = imageRepository;
        _projectClassRepository = projectClassRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating project '{ProjectName}' of type '{ProjectType}' for dataset {DatasetId}",
            request.Name, request.Type, request.DatasetId);

        // Validate dataset exists
        var dataset = await _datasetRepository.GetByIdAsync(request.DatasetId, cancellationToken);
        if (dataset == null)
        {
            _logger.LogWarning("Failed to create project. Dataset with ID {DatasetId} not found", request.DatasetId);
            return Result.Failure<ProjectDto>($"Dataset with ID {request.DatasetId} was not found.");
        }

        // Check for duplicate project name in the same dataset
        var existingProjects = await _projectRepository.GetByDatasetIdAsync(request.DatasetId, cancellationToken);
        if (existingProjects.Any(p => p.Name.Value.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Failed to create project. Project with name '{ProjectName}' already exists in dataset {DatasetId}",
                request.Name, request.DatasetId);
            return Result.Failure<ProjectDto>($"A project with the name '{request.Name}' already exists in this dataset.");
        }

        // Create domain entity
        var project = Project.Create(
            request.Name,
            request.Type,
            request.Description,
            request.DatasetId,
            request.CreatedBy);

        // Persist to repository
        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Successfully created project {ProjectId} with name '{ProjectName}'",
            project.Id, request.Name);

        // Create Role entries with RoleType=None for all images in the dataset
        var images = await _imageRepository.FindAsync(
            new ImageByDatasetSpecification(request.DatasetId),
            cancellationToken);

        foreach (var image in images)
        {
            var role = Role.Create(image.Id, project.Id, RoleType.None.Value);
            await _roleRepository.AddAsync(role, cancellationToken);
        }

        if (request.Type == ProjectType.AnomalyDetection.Value)
        {
            await _projectClassRepository.AddAsync(ProjectClass.Create(
                project.Id,
                0,
                "Normal",
                "#FF0000"));

            await _projectClassRepository.AddAsync(ProjectClass.Create(
                project.Id,
                1,
                "Anomaly",
                "#0000FF"));
        }

        _logger.LogInformation("Created {RoleCount} Role entries with RoleType=None for project {ProjectId}",
            images.Count(), project.Id);

        // Map to DTO and return
        var projectDto = _mapper.Map<ProjectDto>(project);
        return Result.Success(projectDto);
    }
}
