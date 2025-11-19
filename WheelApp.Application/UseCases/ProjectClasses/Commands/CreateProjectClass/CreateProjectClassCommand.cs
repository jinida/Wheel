using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.CreateProjectClass;

/// <summary>
/// Command to create a new project class and return all project classes
/// </summary>
public record CreateProjectClassCommand : ICommand<Result<List<ProjectClassDto>>>
{
    public int ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
}
