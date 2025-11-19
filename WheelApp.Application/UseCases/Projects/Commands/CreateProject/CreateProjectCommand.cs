using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Projects.Commands.CreateProject;

/// <summary>
/// Command to create a new project
/// </summary>
public record CreateProjectCommand : ICommand<Result<ProjectDto>>
{
    public string Name { get; init; } = string.Empty;
    public int DatasetId { get; init; }
    public int Type { get; init; }
    public string? Description { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}
