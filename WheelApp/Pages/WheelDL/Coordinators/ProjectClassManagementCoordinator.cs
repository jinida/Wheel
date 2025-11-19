using MediatR;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for class management lifecycle events
    /// Phase 2 Refactoring - Handles workspace reload after class operations
    /// Refactored to use BaseProjectCoordinator for code reuse
    /// </summary>
    public class ProjectClassManagementCoordinator : BaseProjectCoordinator
    {
        private readonly DrawingToolService _drawingToolService;
        private readonly ClassManagementService _classManagementService;
        private readonly ProjectWorkspaceCoordinator _workspaceCoordinator;
        private readonly ImageSelectionService _imageSelectionService;

        public ProjectClassManagementCoordinator(
            IMediator mediator,
            ProjectWorkspaceService workspaceService,
            DrawingToolService drawingToolService,
            ClassManagementService classManagementService,
            ProjectWorkspaceCoordinator workspaceCoordinator,
            ImageSelectionService imageSelectionService)
            : base(mediator, workspaceService)
        {
            _drawingToolService = drawingToolService;
            _classManagementService = classManagementService;
            _workspaceCoordinator = workspaceCoordinator;
            _imageSelectionService = imageSelectionService;
        }

        /// <summary>
        /// Handles class list changes (create/update)
        /// Reloads workspace and handles pending class selection
        /// Updates selected image reference to point to new workspace object
        /// </summary>
        public async Task<Result> HandleClassesChangedAsync()
        {
            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                // Reload workspace to get updated class list
                var reloadResult = await _workspaceCoordinator.ReloadWorkspaceAsync();

                if (reloadResult.IsSuccess)
                {
                    // Update _selectedImage to point to the new workspace object
                    var selectedImage = _imageSelectionService.SelectedImage;
                    if (selectedImage != null)
                    {
                        var workspace = _workspaceService.CurrentWorkspace;
                        if (workspace != null)
                        {
                            var updatedSelectedImage = workspace.Images.FirstOrDefault(img => img.Id == selectedImage.Id);
                            if (updatedSelectedImage != null)
                            {
                                _imageSelectionService.SelectImage(updatedSelectedImage);
                            }
                        }
                    }

                    // Handle pending class selection if exists
                    await _workspaceCoordinator.HandlePendingClassSelectionAsync();
                }

                return reloadResult;
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error handling class changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles class deletion
        /// Reloads workspace and resets selected class if deleted
        /// </summary>
        public async Task<Result<ClassDeletedResult>> HandleClassDeletedAsync(ProjectClassDto deletedClass)
        {
            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                return Result.Failure<ClassDeletedResult>(contextResult.Error!);
            }

            var context = contextResult.Value!;

            try
            {
                // Reload workspace to reflect deletion
                var reloadResult = await _workspaceCoordinator.ReloadWorkspaceAsync();

                if (!reloadResult.IsSuccess)
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    return Result.Failure<ClassDeletedResult>(
                        GetErrorOrDefault(reloadResult.Error, "Failed to reload workspace after class deletion"));
                }

                // If the deleted class was selected, select first available class
                var workspace = _workspaceService.CurrentWorkspace;
                var currentSelectedClass = _drawingToolService.SelectedClass;

                if (currentSelectedClass != null && currentSelectedClass.Id == deletedClass.Id)
                {
                    if (workspace?.ProjectClasses.Any() == true)
                    {
                        _drawingToolService.SelectClass(workspace.ProjectClasses.First());
                    }
                    else
                    {
                        _drawingToolService.ClearSelectedClass();
                    }
                }

                // NOTE: DO NOT call NotifyClassesChanged() here to avoid duplicate workspace reload
                // ReloadWorkspaceAsync() already updated the workspace and triggered OnWorkspaceLoaded event
                // which updates both RightSideBar and Project page

                return Result.Success(new ClassDeletedResult
                {
                    Message = $"Class '{deletedClass.Name}' deleted successfully",
                    RemainingClassCount = workspace?.ProjectClasses.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<ClassDeletedResult>($"Error handling class deletion: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets pending class selection for auto-selection after class creation
        /// Used when creating a new class and needing to select it immediately
        /// </summary>
        public void SetPendingClassSelection(string className)
        {
            _classManagementService.PendingClassSelection = className;
        }

        /// <summary>
        /// Clears pending class selection
        /// </summary>
        public void ClearPendingClassSelection()
        {
            _classManagementService.ClearPendingSelection();
        }

        /// <summary>
        /// Gets the current pending class name
        /// </summary>
        public string? GetPendingClassName()
        {
            return _classManagementService.PendingClassSelection;
        }

        /// <summary>
        /// Validates if a class can be deleted
        /// Checks if class has any annotations in the project
        /// </summary>
        public bool CanDeleteClass(ProjectClassDto projectClass)
        {
            var workspace = _workspaceService.CurrentWorkspace;
            if (workspace == null)
            {
                return false;
            }

            // Check if any image has annotations with this class
            var hasAnnotations = workspace.Images.Any(img =>
                img.Annotation.Any(ann => ann.classDto?.Id == projectClass.Id));

            return !hasAnnotations;
        }

        /// <summary>
        /// Gets annotation count for a specific class
        /// </summary>
        public int GetClassAnnotationCount(int classId)
        {
            var workspace = _workspaceService.CurrentWorkspace;
            if (workspace == null)
            {
                return 0;
            }

            return workspace.Images
                .SelectMany(img => img.Annotation)
                .Count(ann => ann.classDto?.Id == classId);
        }
    }

    #region Result DTOs

    public class ClassDeletedResult
    {
        public string Message { get; set; } = string.Empty;
        public int RemainingClassCount { get; set; }
    }

    #endregion
}
