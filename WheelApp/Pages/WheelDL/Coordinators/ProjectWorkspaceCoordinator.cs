using MediatR;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Projects.Queries.GetProjectWorkspace;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for project workspace initialization and state management
    /// Phase 2 Refactoring - Extracted from Project.razor.cs
    /// Refactored to use BaseProjectCoordinator for code reuse
    /// </summary>
    public class ProjectWorkspaceCoordinator : BaseProjectCoordinator
    {
        private readonly DrawingToolService _drawingToolService;
        private readonly ClassManagementService _classManagementService;

        public ProjectWorkspaceCoordinator(
            IMediator mediator,
            ProjectWorkspaceService workspaceService,
            DrawingToolService drawingToolService,
            ClassManagementService classManagementService)
            : base(mediator, workspaceService)
        {
            _drawingToolService = drawingToolService;
            _classManagementService = classManagementService;
        }

        /// <summary>
        /// Loads project workspace by ID and initializes state
        /// </summary>
        /// <param name="projectId">Project ID to load</param>
        /// <returns>Result with workspace data</returns>
        public async Task<Result<ProjectWorkspaceDto>> LoadWorkspaceAsync(int projectId)
        {
            // Load workspace data via MediatR
            var result = await _mediator.Send(new GetProjectWorkspaceQuery { ProjectId = projectId });

            if (result.IsSuccess && result.Value != null)
            {
                var workspace = result.Value;

                // IMPORTANT: Set all properties BEFORE calling UpdateWorkspace
                // UpdateWorkspace triggers OnWorkspaceLoaded event, so all properties must be ready

                // Set current project ID
                _workspaceService.SetCurrentProject(projectId);

                // Enable labeling
                _workspaceService.SetLabelability(true);

                // Set project type for UI rendering - cache FirstOrDefault result
                var projectType = workspace.ProjectTypes.FirstOrDefault(pt => pt.Value == workspace.ProjectType);
                _workspaceService.SetProjectType(projectType);

                // Update workspace LAST - this triggers OnWorkspaceLoaded event
                // At this point, CanLabel=true and CurrentProjectType are already set
                _workspaceService.UpdateWorkspace(workspace);

                // RightSideBar now uses ProjectWorkspaceService directly instead of deprecated SidebarStateService
            }

            return result;
        }

        /// <summary>
        /// Reloads current project workspace
        /// Called after class changes or image additions
        /// </summary>
        public async Task<Result> ReloadWorkspaceAsync()
        {
            var currentProjectId = _workspaceService.CurrentProjectId;
            if (!currentProjectId.HasValue)
            {
                return Result.Failure("No project is currently loaded");
            }

            var result = await LoadWorkspaceAsync(currentProjectId.Value);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error ?? "Failed to reload workspace");
        }

        /// <summary>
        /// Handles pending class selection after class creation
        /// Checks ClassManagementService for pending selection and applies it
        /// Sets class silently to avoid triggering OnClassSelected event
        /// </summary>
        public async Task HandlePendingClassSelectionAsync()
        {
            var pendingClassName = _classManagementService.PendingClassSelection;
            if (!string.IsNullOrEmpty(pendingClassName))
            {
                // Find the newly created class
                var workspace = _workspaceService.CurrentWorkspace;
                if (workspace != null)
                {
                    var newClass = workspace.ProjectClasses.FirstOrDefault(c => c.Name == pendingClassName);
                    if (newClass != null)
                    {
                        // Set selected class silently without triggering OnClassSelected event
                        // to avoid changing existing annotations' class
                        _drawingToolService.SetSelectedClassSilently(newClass);
                    }
                }

                // Clear pending selection
                _classManagementService.ClearPendingSelection();
            }
        }
    }
}
