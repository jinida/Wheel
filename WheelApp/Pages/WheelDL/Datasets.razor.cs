using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Datasets.Commands.CreateDataset;
using WheelApp.Application.UseCases.Datasets.Commands.DeleteDatasets;
using WheelApp.Application.UseCases.Datasets.Commands.UpdateDataset;
using WheelApp.Application.UseCases.Datasets.Queries.GetDatasetById;
using WheelApp.Application.UseCases.Datasets.Queries.GetDatasets;
using WheelApp.Application.UseCases.Projects.Commands.CreateProject;
using WheelApp.Application.UseCases.Projects.Commands.DeleteProjects;
using WheelApp.Application.UseCases.Projects.Commands.UpdateProject;
using WheelApp.Application.UseCases.Projects.Queries.GetProjectById;
using WheelApp.Application.UseCases.Projects.Queries.GetProjectsByDataset;
using WheelApp.Application.UseCases.Images.Commands.UploadImages;
using WheelApp.Components;
using WheelApp.State;

namespace WheelApp.Pages.WheelDL
{
    /// <summary>
    /// Datasets page - Coordinator for dataset and project management
    /// Simplified to focus on data orchestration and modal management
    /// </summary>
    public partial class Datasets : ComponentBase
    {
        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ICurrentUserService CurrentUserService { get; set; } = default!;

        // State management
        private readonly PaginationState _datasetPagination = new(pageSize: 5);
        private readonly PaginationState _projectPagination = new(pageSize: 3);
        private readonly SelectionState<int> _datasetSelection = new();
        private readonly SelectionState<int> _projectSelection = new();
        private readonly ModalManager _modalManager = new();

        // Data
        private List<DatasetDto> _datasets = new();
        private List<ProjectDto> _projects = new();

        // Modal references
        private DatasetModal? _datasetModalRef;
        private ProjectModal? _projectModalRef;
        private EditDatasetModal? _editDatasetModalRef;
        private EditProjectModal? _editProjectModalRef;
        private Toast? _toastRef;

        // Upload progress state
        private bool _isUploading = false;
        private string _uploadProgress = "Uploading...";
        private int _uploadedCount = 0;
        private int _totalFiles = 0;

        protected override async Task OnInitializedAsync()
        {
            await LoadDatasets();
        }

        /// <summary>
        /// Load datasets with server-side pagination
        /// </summary>
        private async Task LoadDatasets()
        {
            var query = new GetDatasetsQuery
            {
                PageNumber = _datasetPagination.CurrentPage,
                PageSize = _datasetPagination.PageSize
            };

            var result = await Mediator.Send(query);

            if (result.IsSuccess && result.Value != null)
            {
                _datasets = result.Value.Items.ToList();

                bool pageCorrected = _datasetPagination.UpdatePagination(
                    result.Value.TotalCount,
                    result.Value.TotalPages
                );

                if (pageCorrected)
                {
                    query = new GetDatasetsQuery
                    {
                        PageNumber = _datasetPagination.CurrentPage,
                        PageSize = _datasetPagination.PageSize
                    };

                    result = await Mediator.Send(query);

                    if (result.IsSuccess && result.Value != null)
                    {
                        _datasets = result.Value.Items.ToList();
                        _datasetPagination.UpdatePagination(
                            result.Value.TotalCount,
                            result.Value.TotalPages
                        );
                    }
                }
            }
            else
            {
                _datasets = new List<DatasetDto>();
                _datasetPagination.UpdatePagination(0, 1);
                ShowToast("Load Failed", "Failed to load datasets.", "error");
            }
        }

        /// <summary>
        /// Load projects for a specific dataset
        /// </summary>
        private async Task LoadProjectsForDataset(int datasetId)
        {
            var query = new GetProjectsByDatasetQuery { 
                DatasetId = datasetId, 
                PageNumber = _projectPagination.CurrentPage, 
                PageSize = _projectPagination.PageSize
            };

            var result = await Mediator.Send(query);

            if (result.IsSuccess && result.Value != null)
            {
                _projects = result.Value.Items.ToList();

                bool pageCorrected = _projectPagination.UpdatePagination(
                    result.Value.TotalCount,
                    result.Value.TotalPages
                );

                if (pageCorrected)
                {
                    query = new GetProjectsByDatasetQuery
                    {
                        DatasetId = datasetId,
                        PageNumber = _projectPagination.CurrentPage,
                        PageSize = _projectPagination.PageSize
                    }; 

                    result = await Mediator.Send(query);

                    if (result.IsSuccess && result.Value != null)
                    {
                        _projects = result.Value.Items.ToList();
                        _projectPagination.UpdatePagination(
                            result.Value.TotalCount,
                            result.Value.TotalPages
                        );
                    }
                }
            }
            else
            {
                _projects = new List<ProjectDto>();
                _projectPagination.UpdatePagination(0, 1);
                ShowToast("Load Failed", "Failed to load projects for dataset.", "error");
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Handle dataset double-click - toggle selection
        /// </summary>
        private async Task HandleDatasetDoubleClick(int datasetId)
        {
            if (_datasetSelection.HighlightedId == datasetId)
            {
                _datasetSelection.ClearHighlight();
                _projectSelection.Clear();
                _projects.Clear();
            }
            else
            {
                _datasetSelection.Highlight(datasetId);
                _projectSelection.ClearHighlight();
                _projectPagination.Reset();
                await LoadProjectsForDataset(datasetId);
            }
        }

        /// <summary>
        /// Change dataset page
        /// </summary>
        private async Task ChangeDatasetPage(int pageNumber)
        {
            if (_datasetPagination.GoToPage(pageNumber))
            {
                await LoadDatasets();
            }
        }

        /// <summary>
        /// Change project page
        /// </summary>
        private async Task ChangeProjectPage(int pageNumber)
        {
            if (_projectPagination.GoToPage(pageNumber))
            {
                if (_datasetSelection.HighlightedId.HasValue)
                {
                    await LoadProjectsForDataset(_datasetSelection.HighlightedId.Value);
                }
            }
        }

        /// <summary>
        /// Delete selected datasets
        /// </summary>
        private async Task DeleteSelectedDatasets()
        {
            if (!_datasetSelection.HasSelection) return;

            bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
                $"Are you sure you want to delete {_datasetSelection.SelectionCount} dataset(s)?");

            if (!confirmed) return;

            var command = new DeleteDatasetsCommand
            {
                Ids = _datasetSelection.SelectedIds.ToList()
            };

            var result = await Mediator.Send(command);

            // Clear highlighted if it was in the selection
            if (_datasetSelection.HighlightedId.HasValue &&
                _datasetSelection.IsSelected(_datasetSelection.HighlightedId.Value))
            {
                _datasetSelection.ClearHighlight();
                _projectSelection.Clear();
                _projects.Clear();
            }

            _datasetSelection.ClearSelection();
            await LoadDatasets();

            if (result.IsSuccess)
            {
                ShowToast("Datasets Deleted", $"{result.Value} dataset(s) deleted successfully.", "success");
            }
            else
            {
                ShowToast("Delete Failed", "Failed to delete datasets.", "error");
            }
        }

        /// <summary>
        /// Delete selected projects
        /// </summary>
        private async Task DeleteSelectedProjects()
        {
            if (!_projectSelection.HasSelection) return;

            bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
                $"Are you sure you want to delete {_projectSelection.SelectionCount} project(s)?");

            if (!confirmed) return;

            var command = new DeleteProjectsCommand
            {
                Ids = _projectSelection.SelectedIds.ToList()
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                // Clear highlighted project if it was in the selection
                var deletedIds = _projectSelection.SelectedIds.ToList();
                if (_projectSelection.HighlightedId.HasValue &&
                    deletedIds.Contains(_projectSelection.HighlightedId.Value))
                {
                    _projectSelection.ClearHighlight();
                }

                _projectSelection.ClearSelection();

                // Reload projects from server to ensure correct pagination
                if (_datasetSelection.HighlightedId.HasValue)
                {
                    await LoadProjectsForDataset(_datasetSelection.HighlightedId.Value);
                }

                ShowToast("Projects Deleted", $"{result.Value} project(s) deleted successfully.", "success");
            }
            else
            {
                ShowToast("Delete Failed", result.Error, "error");
            }
        }

        /// <summary>
        /// Navigate to project labeling page
        /// </summary>
        private void HandleNextButtonClick()
        {
            if (_projectSelection.HighlightedId.HasValue)
            {
                NavigationManager.NavigateTo($"/wheeldl/project/{_projectSelection.HighlightedId.Value}");
            }
        }

        // Modal visibility properties
        private bool _isDatasetModalVisible => _modalManager.IsVisible(ModalManager.ModalKeys.CreateDataset);
        private bool _isProjectModalVisible => _modalManager.IsVisible(ModalManager.ModalKeys.CreateProject);
        private bool _isEditDatasetModalVisible => _modalManager.IsVisible(ModalManager.ModalKeys.EditDataset);
        private bool _isEditProjectModalVisible => _modalManager.IsVisible(ModalManager.ModalKeys.EditProject);

        // Modal methods
        private void HideCreateDatasetModal()
        {
            _modalManager.Hide(ModalManager.ModalKeys.CreateDataset);
            _datasetModalRef?.ClearFiles();
        }

        private async Task ShowEditDatasetModal(int datasetId)
        {
            var query = new GetDatasetByIdQuery { Id = datasetId };
            var result = await Mediator.Send(query);

            if (result.IsSuccess && result.Value != null)
            {
                _editDatasetModalRef?.Initialize(result.Value.Id, result.Value.Name, result.Value.Description);
                _modalManager.Show(ModalManager.ModalKeys.EditDataset);
            }
            else
            {
                ShowToast("Load Failed", "Failed to load dataset details.", "error");
            }
        }

        private void HideEditDatasetModal() => _modalManager.Hide(ModalManager.ModalKeys.EditDataset);

        private void HideCreateProjectModal() => _modalManager.Hide(ModalManager.ModalKeys.CreateProject);

        private async Task ShowEditProjectModal(int projectId)
        {
            var query = new GetProjectByIdQuery { Id = projectId };
            var result = await Mediator.Send(query);

            if (result.IsSuccess && result.Value != null)
            {
                _editProjectModalRef?.Initialize(result.Value.Id, result.Value.Name, result.Value.Description);
                _modalManager.Show(ModalManager.ModalKeys.EditProject);
            }
            else
            {
                ShowToast("Load Failed", "Failed to load project details.", "error");
            }
        }

        private void HideEditProjectModal() => _modalManager.Hide(ModalManager.ModalKeys.EditProject);

        /// <summary>
        /// Handle dataset creation with images
        /// </summary>
        private async Task HandleDatasetCreated(DatasetModal.Model model)
        {
            var fileInfos = new List<FileUploadInfo>();

            try
            {
                if (model.UploadedFiles != null && model.UploadedFiles.Any())
                {
                    _isUploading = true;
                    _totalFiles = model.UploadedFiles.Count;
                    _uploadedCount = 0;
                    _uploadProgress = "Preparing files...";
                    StateHasChanged();

                    foreach (var file in model.UploadedFiles)
                    {
                        using var browserStream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                        var memoryStream = new MemoryStream();
                        await browserStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        fileInfos.Add(new FileUploadInfo
                        {
                            FileName = file.Name,
                            Stream = memoryStream,
                            FileSize = file.Size
                        });
                    }
                }

                var progress = new Progress<UploadProgressInfo>(progressInfo =>
                {
                    _uploadedCount = progressInfo.ProcessedFiles;
                    _uploadProgress = progressInfo.Message;
                    StateHasChanged();
                });

                var command = new CreateDatasetCommand
                {
                    Name = model.DatasetName ?? "",
                    Description = model.Description,
                    CreatedBy = CurrentUserService.UserName ?? "Anonymous",
                    Files = fileInfos,
                    ProgressCallback = progress
                };

                var result = await Mediator.Send(command);

                if (!result.IsSuccess)
                {
                    if (result.Error.Contains("already exists"))
                    {
                        _datasetModalRef?.SetValidationError(result.Error);
                    }
                    return;
                }

                HideCreateDatasetModal();
                await LoadDatasets();
                if (result.Value != null && result.Value.SuccessfulUploads > 0)
                {
                    ShowToast("Dataset Created",
                        $"Dataset '{result.Value.DatasetName}' created with {result.Value.SuccessfulUploads} image(s)!",
                        "success");
                }
                else
                {
                    ShowToast("Dataset Created", "Dataset created successfully!", "success");
                }
            }
            finally
            {
                foreach (var fileInfo in fileInfos)
                {
                    await fileInfo.Stream.DisposeAsync();
                }
                _isUploading = false;
                await Task.Delay(300);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Handle project creation
        /// </summary>
        private async Task HandleProjectCreated(ProjectModal.Model model)
        {
            if (!_datasetSelection.HighlightedId.HasValue) return;

            var command = new CreateProjectCommand
            {
                Name = model.ProjectName ?? "",
                Description = model.Description,
                DatasetId = _datasetSelection.HighlightedId.Value,
                Type = model.ProjectType ?? 0,
                CreatedBy = CurrentUserService.UserName ?? "Anonymous"
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                HideCreateProjectModal();
                await LoadProjectsForDataset(_datasetSelection.HighlightedId.Value);
                ShowToast("Project Created", "Project created successfully!", "success");
            }
            else if (result.Error.Contains("already exists"))
            {
                _projectModalRef?.SetValidationError(result.Error);
            }
        }

        /// <summary>
        /// Handle dataset update
        /// </summary>
        private async Task HandleDatasetUpdated(EditDatasetModal.Model model)
        {
            var command = new UpdateDatasetCommand
            {
                Id = model.Id,
                Name = model.DatasetName ?? "",
                Description = model.Description,
                ModifiedBy = CurrentUserService.UserName ?? "Anonymous"
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                HideEditDatasetModal();
                await LoadDatasets();
                ShowToast("Dataset Updated", "Dataset updated successfully!", "success");
            }
            else if (result.Error.Contains("already exists"))
            {
                _editDatasetModalRef?.SetValidationError(result.Error);
            }
        }

        /// <summary>
        /// Handle project update
        /// </summary>
        private async Task HandleProjectUpdated(EditProjectModal.Model model)
        {
            var command = new UpdateProjectCommand
            {
                Id = model.Id,
                Name = model.ProjectName ?? "",
                Description = model.Description,
                ModifiedBy = CurrentUserService.UserName ?? "Anonymous"
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                HideEditProjectModal();
                if (_datasetSelection.HighlightedId.HasValue)
                {
                    await LoadProjectsForDataset(_datasetSelection.HighlightedId.Value);
                }
                ShowToast("Project Updated", "Project updated successfully!", "success");
            }
            else if (result.Error.Contains("already exists"))
            {
                _editProjectModalRef?.SetValidationError(result.Error);
            }
        }

        /// <summary>
        /// Handle dataset checkbox selection toggle
        /// </summary>
        private void HandleDatasetSelectionToggle(int datasetId)
        {
            _datasetSelection.Toggle(datasetId);
            StateHasChanged();
        }

        /// <summary>
        /// Handle toggle all datasets checkbox
        /// </summary>
        private void HandleToggleAllDatasets()
        {
            var allDatasetIds = _datasets.Select(d => d.Id);
            _datasetSelection.ToggleAll(allDatasetIds);
            StateHasChanged();
        }

        /// <summary>
        /// Handle project checkbox selection toggle
        /// </summary>
        private void HandleProjectSelectionToggle(int projectId)
        {
            _projectSelection.Toggle(projectId);
            StateHasChanged();
        }

        /// <summary>
        /// Handle toggle all projects checkbox
        /// </summary>
        private void HandleToggleAllProjects()
        {
            var allProjectIds = _projects.Select(p => p.Id);
            _projectSelection.ToggleAll(allProjectIds);
            StateHasChanged();
        }

        /// <summary>
        /// Centralized toast notification helper
        /// </summary>
        private void ShowToast(string title, string message, string type)
        {
            _toastRef?.Show(title, message, type);
        }
    }
}
