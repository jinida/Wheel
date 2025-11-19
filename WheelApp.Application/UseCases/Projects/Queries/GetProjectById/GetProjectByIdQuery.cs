using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Projects.Queries.GetProjectById;

/// <summary>
/// Query to get a single project by ID
/// </summary>
public record GetProjectByIdQuery : IQuery<Result<ProjectDto>>
{
    public int Id { get; init; }
}
