using MediatR;
using Microsoft.JSInterop;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotations;
using WheelApp.Application.UseCases.ProjectClasses.Commands.CreateProjectClass;
using WheelApp.Application.UseCases.ProjectClasses.Commands.DeleteProjectClass;
using WheelApp.Application.UseCases.ProjectClasses.Commands.UpdateProjectClass;
using WheelApp.Application.UseCases.Projects.Commands.InitializeProject;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for RightSideBar operations
    /// Handles Class and Annotation CRUD operations
    /// Separates business logic from UI presentation
    /// </summary>
    public class RightSideBarCoordinator : BaseProjectCoordinator
    {
        private readonly ProjectClassManagementCoordinator _classManagementCoordinator;
        private readonly ProjectAnnotationCoordinator _annotationCoordinator;
        private readonly ProjectWorkspaceCoordinator _workspaceCoordinator;
        private readonly ProjectRoleCoordinator _roleCoordinator;

        public RightSideBarCoordinator(
            IMediator mediator,
            ProjectWorkspaceService workspaceService,
            ProjectClassManagementCoordinator classManagementCoordinator,
            ProjectAnnotationCoordinator annotationCoordinator,
            ProjectWorkspaceCoordinator workspaceCoordinator,
            ProjectRoleCoordinator roleCoordinator)
            : base(mediator, workspaceService)
        {
            _classManagementCoordinator = classManagementCoordinator;
            _annotationCoordinator = annotationCoordinator;
            _workspaceCoordinator = workspaceCoordinator;
            _roleCoordinator = roleCoordinator;
        }

        #region Class Management

        /// <summary>
        /// Creates a new project class
        /// </summary>
        public async Task<Result<ProjectClassDto>> CreateClassAsync(string className, string classColor)
        {
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<ProjectClassDto>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            var command = new CreateProjectClassCommand
            {
                ProjectId = context.ProjectId,
                Name = className,
                Color = classColor
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Value != null && result.Value.Count > 0)
            {
                // Set pending selection for the newly created class
                _classManagementCoordinator.SetPendingClassSelection(className);

                // Trigger workspace reload
                await _classManagementCoordinator.HandleClassesChangedAsync();

                // Extract the first (newly created) class from the list
                return Result.Success(result.Value[0]);
            }

            return Result.Failure<ProjectClassDto>(result.Error ?? "Failed to create class");
        }

        /// <summary>
        /// Updates an existing project class
        /// </summary>
        public async Task<Result<ProjectClassDto>> UpdateClassAsync(int classId, string className, string classColor)
        {
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<ProjectClassDto>(contextResult.Error!);
            }

            var command = new UpdateProjectClassCommand
            {
                Id = classId,
                Name = className,
                Color = classColor
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // Update workspace with the modified class
                var workspace = _workspaceService.CurrentWorkspace;
                if (workspace != null && result.Value != null)
                {
                    var existingClass = workspace.ProjectClasses.FirstOrDefault(c => c.Id == result.Value.Id);
                    if (existingClass != null)
                    {
                        existingClass.Name = result.Value.Name;
                        existingClass.Color = result.Value.Color;
                        existingClass.ClassIdx = result.Value.ClassIdx;
                    }
                }

                await _classManagementCoordinator.HandleClassesChangedAsync();
            }

            return result;
        }

        /// <summary>
        /// Deletes a project class
        /// </summary>
        public async Task<Result<List<ProjectClassDto>>> DeleteClassAsync(ProjectClassDto classToDelete)
        {
            var command = new DeleteProjectClassCommand { Id = classToDelete.Id };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                await _classManagementCoordinator.HandleClassDeletedAsync(classToDelete);
            }

            return result;
        }

        /// <summary>
        /// Gets annotation count for a class
        /// </summary>
        public int GetClassAnnotationCount(int classId)
        {
            return _classManagementCoordinator.GetClassAnnotationCount(classId);
        }

        #endregion

        #region Annotation Management

        /// <summary>
        /// Deletes a single annotation
        /// </summary>
        public async Task<Result> DeleteAnnotationAsync(int annotationId)
        {
            return await _annotationCoordinator.DeleteAnnotationAsync(annotationId);
        }

        /// <summary>
        /// Deletes multiple annotations
        /// </summary>
        public async Task<Result> DeleteAnnotationsBatchAsync(List<int> annotationIds)
        {
            return await _annotationCoordinator.DeleteAnnotationsBatchAsync(annotationIds);
        }

        #endregion

        #region Project Operations

        /// <summary>
        /// Initializes project by removing all annotations and roles
        /// Reloads workspace after completion to refresh UI
        /// </summary>
        public async Task<Result<InitializeProjectResult>> InitializeProjectAsync()
        {
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<InitializeProjectResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            var command = new InitializeProjectCommand
            {
                ProjectId = context.ProjectId
            };

            var result = await _mediator.Send(command);

            // Reload workspace after initialization to refresh UI
            if (result.IsSuccess)
            {
                await _workspaceCoordinator.ReloadWorkspaceAsync();
            }

            return result;
        }

        /// <summary>
        /// Performs random split of images into Train/Validation/Test roles
        /// Memory state is already updated by ProjectRoleCoordinator, just notify UI
        /// </summary>
        public async Task<Result<RandomSplitResult>> PerformRandomSplitAsync()
        {
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<RandomSplitResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            var result = await _roleCoordinator.PerformRandomSplitAsync(context.ProjectId);

            // Memory state already updated, just trigger OnWorkspaceLoaded to refresh UI
            if (result.IsSuccess)
            {
                _workspaceService.TriggerWorkspaceLoaded();
            }

            return result;
        }

        #endregion
    }
}
