using MediatR;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Base coordinator providing common functionality for all project coordinators
    /// Eliminates code duplication and provides consistent patterns
    /// </summary>
    public abstract class BaseProjectCoordinator
    {
        protected readonly IMediator _mediator;
        protected readonly ProjectWorkspaceService _workspaceService;

        protected BaseProjectCoordinator(
            IMediator mediator,
            ProjectWorkspaceService workspaceService)
        {
            _mediator = mediator;
            _workspaceService = workspaceService;
        }

        /// <summary>
        /// Gets validated workspace context (workspace + projectId)
        /// Eliminates 15+ duplicate validation patterns
        /// </summary>
        protected Result<WorkspaceContext> GetValidatedWorkspaceContext()
        {
            var workspace = _workspaceService.CurrentWorkspace;
            if (workspace == null)
            {
                return Result.Failure<WorkspaceContext>("No workspace loaded");
            }

            var projectId = _workspaceService.CurrentProjectId;
            if (!projectId.HasValue)
            {
                return Result.Failure<WorkspaceContext>("No project loaded");
            }

            return Result<WorkspaceContext>.Success(new WorkspaceContext
            {
                Workspace = workspace,
                ProjectId = projectId.Value
            });
        }

        /// <summary>
        /// Checks if project is Anomaly Detection type
        /// Eliminates 3+ duplicate checks
        /// </summary>
        protected static bool IsAnomalyDetectionProject(ProjectWorkspaceDto workspace)
        {
            return workspace.ProjectType == 3;
        }

        /// <summary>
        /// Gets Normal class for Anomaly Detection projects (ClassIdx == 0)
        /// Eliminates 3+ duplicate lookups
        /// </summary>
        protected static ProjectClassDto? GetNormalClass(ProjectWorkspaceDto workspace)
        {
            return workspace.ProjectClasses.FirstOrDefault(c => c.ClassIdx == 0);
        }

        /// <summary>
        /// Checks if a class is the Normal class
        /// </summary>
        protected static bool IsNormalClass(ProjectWorkspaceDto workspace, int classId)
        {
            var normalClass = GetNormalClass(workspace);
            return normalClass?.Id == classId;
        }

        /// <summary>
        /// Creates image dictionary for O(1) lookups
        /// Eliminates 6+ duplicate dictionary creations
        /// </summary>
        protected static Dictionary<int, ImageDto> CreateImageDictionary(ProjectWorkspaceDto workspace)
        {
            return workspace.Images.ToDictionary(i => i.Id, i => i);
        }

        /// <summary>
        /// Creates role type dictionary for O(1) lookups
        /// Eliminates 6+ duplicate dictionary creations
        /// </summary>
        protected static Dictionary<string, RoleTypeDto> CreateRoleTypeDictionary(ProjectWorkspaceDto workspace)
        {
            return workspace.RoleTypes.ToDictionary(r => r.Name, r => r);
        }

        /// <summary>
        /// Formats a list with truncation (e.g., "item1, item2, item3 and 5 more")
        /// Eliminates 2+ duplicate formatting patterns
        /// </summary>
        protected static string FormatTruncatedList(List<string> items, int maxDisplay = 3)
        {
            if (items.Count <= maxDisplay)
            {
                return string.Join(", ", items);
            }

            var displayed = string.Join(", ", items.Take(maxDisplay));
            var remaining = items.Count - maxDisplay;
            return $"{displayed} and {remaining} more";
        }

        /// <summary>
        /// Gets error message with fallback
        /// Eliminates 10+ duplicate null-coalescing patterns
        /// </summary>
        protected static string GetErrorOrDefault(string? error, string defaultMessage)
        {
            return error ?? defaultMessage;
        }
    }

    /// <summary>
    /// Workspace context containing validated workspace and project ID
    /// </summary>
    public class WorkspaceContext
    {
        public ProjectWorkspaceDto Workspace { get; set; } = null!;
        public int ProjectId { get; set; }
    }
}
