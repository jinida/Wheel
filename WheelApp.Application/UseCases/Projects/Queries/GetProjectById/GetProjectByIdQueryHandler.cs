using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Exceptions;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Projects.Queries.GetProjectById;

/// <summary>
/// Handles retrieving a single project by ID
/// </summary>
public class GetProjectByIdQueryHandler : IQueryHandler<GetProjectByIdQuery, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProjectByIdQueryHandler> _logger;
    public GetProjectByIdQueryHandler(
        IProjectRepository projectRepository,
        IMapper mapper, 
        ILogger<GetProjectByIdQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ProjectDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        // Fetch project by ID
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null)
        {
            return Result.Failure<ProjectDto>($"Project with ID {request.Id} was not found.");
        }

        // Map to DTO and return
        var projectDto = _mapper.Map<ProjectDto>(project);
        return Result.Success(projectDto);
    }
}
