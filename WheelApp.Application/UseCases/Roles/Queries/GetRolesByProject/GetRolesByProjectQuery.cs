using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Roles.Queries.GetRolesByProject;

/// <summary>
/// Query to get all roles for a specific project
/// </summary>
public record GetRolesByProjectQuery : IQuery<Result<List<RoleDto>>>
{
    public int ProjectId { get; init; }
}
