using MediatR;
using WheelApp.Application.Common.Models;
using WheelApp.Application.UseCases.Roles.Commands.RandomSplitRoles;
using WheelApp.Application.UseCases.Roles.Commands.UpdateRoleByIds;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for image role assignment (Train/Validation/Test)
    /// Phase 2 Refactoring - Extracted from Project.razor.cs
    /// Refactored to use BaseProjectCoordinator for code reuse
    /// </summary>
    public class ProjectRoleCoordinator : BaseProjectCoordinator
    {
        private readonly ProjectWorkspaceCoordinator _workspaceCoordinator;

        public ProjectRoleCoordinator(
            IMediator mediator,
            ProjectWorkspaceService workspaceService,
            ProjectWorkspaceCoordinator workspaceCoordinator)
            : base(mediator, workspaceService)
        {
            _workspaceCoordinator = workspaceCoordinator;
        }

        /// <summary>
        /// Sets role for selected images
        /// RoleType values: 0=Train, 1=Validation, 2=Test, 3=None
        /// </summary>
        public async Task<Result<SetRoleResult>> SetRoleAsync(List<int> imageIds, int roleType)
        {
            if (imageIds.Count == 0)
            {
                return Result.Failure<SetRoleResult>("No images selected");
            }

            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<SetRoleResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                // Send command to set roles
                var command = new UpdateRoleByIdsCommand
                {
                    ProjectId = context.ProjectId,
                    ImageIds = imageIds,
                    RoleValue = roleType
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess && result.Value != null)
                {
                    // Use base class helpers to create dictionaries (eliminates duplication)
                    var imageDict = CreateImageDictionary(context.Workspace);
                    var roleTypeDict = CreateRoleTypeDictionary(context.Workspace);

                    // Check Anomaly Detection business rules using base class helpers
                    int normalToValidationCount = 0;
                    bool isAnomalyDetection = IsAnomalyDetectionProject(context.Workspace);
                    var normalClass = isAnomalyDetection ? GetNormalClass(context.Workspace) : null;

                    // Update local state efficiently using dictionary lookups
                    foreach (var imageId in imageIds)
                    {
                        if (imageDict.TryGetValue(imageId, out var image) &&
                            result.Value.UpdatedRoles.TryGetValue(imageId, out var roleName) &&
                            roleTypeDict.TryGetValue(roleName, out var roleDto))
                        {
                            // Check if this was a Normal class image that was changed to Validation instead of Training
                            if (isAnomalyDetection && roleType == 0 && roleName == "Validation" && normalClass != null)
                            {
                                // Check if this image has Normal class annotation
                                var hasNormalClass = image.Annotation?.Any(a => a.classDto?.Id == normalClass.Id) ?? false;
                                if (hasNormalClass)
                                {
                                    normalToValidationCount++;
                                }
                            }

                            image.RoleType = roleDto;
                        }
                    }

                    return Result<SetRoleResult>.Success(new SetRoleResult
                    {
                        NormalToValidationCount = normalToValidationCount,
                        FailedCount = result.Value.FailedImageIds?.Count ?? 0,
                        UpdatedCount = result.Value.CreatedCount + result.Value.UpdatedCount,
                        Message = "Role set successfully"
                    });
                }
                else
                {
                    // Use base class helper for error fallback
                    return Result.Failure<SetRoleResult>(GetErrorOrDefault(result.Error, "Failed to set role"));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<SetRoleResult>($"Error setting role: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs random split of all images into Train/Validation/Test
        /// Uses default ratios defined in the command
        /// </summary>
        public async Task<Result<RandomSplitResult>> PerformRandomSplitAsync(int projectId)
        {
            // Use base class validation
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<RandomSplitResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                var command = new RandomSplitRolesCommand
                {
                    ProjectId = projectId
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess && result.Value != null)
                {
                    // Use base class helpers to create dictionaries (prevents O(n²) issue)
                    var imageDict = CreateImageDictionary(context.Workspace);
                    var roleTypeDict = CreateRoleTypeDictionary(context.Workspace);

                    foreach (var kvp in result.Value.UpdatedRoles)
                    {
                        if (imageDict.TryGetValue(kvp.Key, out var image) &&
                            roleTypeDict.TryGetValue(kvp.Value, out var roleDto))
                        {
                            image.RoleType = roleDto;
                        }
                    }

                    return Result.Success(new RandomSplitResult
                    {
                        Message = result.Value.Message,
                        UpdatedRoles = result.Value.UpdatedRoles
                    });
                }
                else
                {
                    return Result.Failure<RandomSplitResult>(GetErrorOrDefault(result.Error, "Failed to perform random split"));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<RandomSplitResult>($"Error during random split: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes all image roles to None (0)
        /// </summary>
        public async Task<Result> InitializeAllRolesAsync(int projectId)
        {
            // Use base class validation
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                var allImageIds = context.Workspace.Images.Select(i => i.Id).ToList();

                if (allImageIds.Count == 0)
                {
                    return Result.Failure("No images in project");
                }

                // Set all to None (value = 3)
                var command = new UpdateRoleByIdsCommand
                {
                    ProjectId = projectId,
                    ImageIds = allImageIds,
                    RoleValue = 3 // None
                };

                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    // Reload workspace to reflect changes
                    await _workspaceCoordinator.ReloadWorkspaceAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error initializing roles: {ex.Message}");
            }
        }
    }

    #region Result DTOs

    public class SetRoleResult
    {
        public int NormalToValidationCount { get; set; }
        public int FailedCount { get; set; }
        public int UpdatedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RandomSplitResult
    {
        public string Message { get; set; } = string.Empty;
        public Dictionary<int, string> UpdatedRoles { get; set; } = new();
    }

    #endregion
}
