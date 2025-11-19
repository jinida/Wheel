using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Projects.Queries.GetProjectWorkspace;

/// <summary>
/// Query to get complete project workspace data for labeling/annotation interface
/// Includes project details, classes, images, roles, and annotations
/// </summary>
public record GetProjectWorkspaceQuery : IQuery<Result<ProjectWorkspaceDto>>
{
    public int ProjectId { get; init; }
}
