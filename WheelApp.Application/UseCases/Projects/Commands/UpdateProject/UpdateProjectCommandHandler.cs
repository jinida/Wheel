using AutoMapper;
using WheelApp.Application.Common.Exceptions;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Projects.Commands.UpdateProject;

/// <summary>
/// Handles the update of an existing project
/// </summary>
public class UpdateProjectCommandHandler : ICommandHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _mapper = mapper;
    }

    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        // Fetch the existing project
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null)
        {
            return Result.Failure<ProjectDto>($"Project with ID {request.Id} was not found.");
        }

        // Check if another project with the same name exists in the same dataset (excluding current project)
        var existingProjects = await _projectRepository.GetByDatasetIdAsync(project.DatasetId, cancellationToken);
        if (existingProjects.Any(p => p.Id != request.Id && p.Name.Value.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<ProjectDto>($"A project with the name '{request.Name}' already exists in this dataset.");
        }

        // Update domain entity
        project.UpdateName(request.Name, request.ModifiedBy);
        project.UpdateDescription(request.Description, request.ModifiedBy);

        // Changes are saved automatically by TransactionBehavior

        // Map to DTO and return
        var projectDto = _mapper.Map<ProjectDto>(project);
        return Result.Success(projectDto);
    }
}
