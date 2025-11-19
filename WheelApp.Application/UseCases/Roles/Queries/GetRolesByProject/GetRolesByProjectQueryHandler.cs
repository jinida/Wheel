using AutoMapper;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Roles.Queries.GetRolesByProject;

/// <summary>
/// Handles retrieving all roles for a specific project
/// </summary>
public class GetRolesByProjectQueryHandler : IQueryHandler<GetRolesByProjectQuery, Result<List<RoleDto>>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;

    public GetRolesByProjectQueryHandler(
        IRoleRepository roleRepository,
        IProjectRepository projectRepository,
        IMapper mapper)
    {
        _roleRepository = roleRepository;
        _projectRepository = projectRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<RoleDto>>> Handle(GetRolesByProjectQuery request, CancellationToken cancellationToken)
    {
        // Validate project exists
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return Result.Failure<List<RoleDto>>($"Project with ID {request.ProjectId} does not exist.");
        }

        // Get roles for the project
        var roles = await _roleRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

        // Map to DTOs
        var roleDtos = _mapper.Map<List<RoleDto>>(roles);

        return Result.Success(roleDtos);
    }
}
