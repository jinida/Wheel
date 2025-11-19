using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Roles.Commands.UpdateRoleByIds;

/// <summary>
/// Command to update roles for multiple images in a project
/// </summary>
public class UpdateRoleByIdsCommand : ICommand<Result<UpdateRoleByIdsResult>>
{
    /// <summary>
    /// The project ID
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// List of image IDs to update roles for
    /// </summary>
    public List<int> ImageIds { get; set; } = new();

    /// <summary>
    /// The role value to assign (0: Train, 1: Validation, 2: Test, 3: None, null: remove role)
    /// </summary>
    public int? RoleValue { get; set; }
}

/// <summary>
/// Result of the bulk role update operation
/// </summary>
public class UpdateRoleByIdsResult
{
    /// <summary>
    /// Number of roles created
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of roles updated
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of roles removed
    /// </summary>
    public int RemovedCount { get; set; }

    /// <summary>
    /// List of image IDs that failed to update
    /// </summary>
    public List<int> FailedImageIds { get; set; } = new();

    /// <summary>
    /// Updated role information for each image
    /// </summary>
    public Dictionary<int, string> UpdatedRoles { get; set; } = new();
}