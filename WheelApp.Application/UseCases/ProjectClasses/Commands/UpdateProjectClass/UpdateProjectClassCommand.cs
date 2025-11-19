using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.UpdateProjectClass;

/// <summary>
/// Command to update a project class
/// </summary>
public record UpdateProjectClassCommand : ICommand<Result<ProjectClassDto>>
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
}
