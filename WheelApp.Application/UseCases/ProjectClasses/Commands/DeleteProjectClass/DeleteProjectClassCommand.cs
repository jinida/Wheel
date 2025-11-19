using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.DeleteProjectClass;

/// <summary>
/// Command to delete a project class and return all remaining classes
/// </summary>
public record DeleteProjectClassCommand : ICommand<Result<List<ProjectClassDto>>>
{
    public int Id { get; init; }
}
