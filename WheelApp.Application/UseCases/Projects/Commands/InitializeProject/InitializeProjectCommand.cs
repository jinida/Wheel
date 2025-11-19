using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Projects.Commands.InitializeProject;

/// <summary>
/// Command to initialize a project by removing all annotations and roles
/// </summary>
public class InitializeProjectCommand : ICommand<Result<InitializeProjectResult>>
{
    /// <summary>
    /// The ID of the project to initialize
    /// </summary>
    public int ProjectId { get; set; }
}

/// <summary>
/// Result of initializing a project
/// </summary>
public class InitializeProjectResult
{
    /// <summary>
    /// Number of annotations deleted
    /// </summary>
    public int AnnotationsDeleted { get; set; }

    /// <summary>
    /// Number of roles deleted
    /// </summary>
    public int RolesDeleted { get; set; }

    /// <summary>
    /// Total number of images affected
    /// </summary>
    public int ImagesAffected { get; set; }

    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Optional message about the operation
    /// </summary>
    public string? Message { get; set; }
}