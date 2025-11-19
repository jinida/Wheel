using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Projects.Commands.DeleteProjects;

/// <summary>
/// Command to bulk delete multiple projects
/// Provides better performance and transaction consistency than individual deletes
/// </summary>
public record DeleteProjectsCommand : ICommand<Result<int>>
{
    /// <summary>
    /// List of project IDs to delete
    /// </summary>
    public List<int> Ids { get; init; } = new();
}
