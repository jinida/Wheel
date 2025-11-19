using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Projects.Commands.UpdateProject;

/// <summary>
/// Command to update an existing project
/// </summary>
public record UpdateProjectCommand : ICommand<Result<ProjectDto>>
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}
