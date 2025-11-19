using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.ProjectClasses.Queries.GetProjectClassesByProject;

/// <summary>
/// Query to get all classes for a specific project
/// </summary>
public record GetProjectClassesByProjectQuery : IQuery<Result<List<ProjectClassDto>>>
{
    public int ProjectId { get; init; }
}
