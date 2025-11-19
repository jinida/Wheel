using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WheelApp.Application.UseCases.Projects.Queries.GetTrainingStatistics;
using WheelApp.Components;
using WheelApp.Pages.WheelDL.Coordinators;

namespace WheelApp.Pages.WheelDL
{
    public partial class Training : ComponentBase
    {
        [Parameter]
        public string? Id { get; set; }

        [Inject]
        private IMediator Mediator { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        private TrainingCoordinator TrainingCoordinator { get; set; } = default!;

        private bool _isLoading = true;
        private bool _isCreatingTraining = false;
        private TrainingStatisticsDto? _statistics;
        private string? _trainingName;

        private Dictionary<string, int> _classDistributionData = new();
        private Dictionary<string, int> _splitDistributionData = new();
        private List<string> _classColors = new();

        protected override async Task OnInitializedAsync()
        {
            _isLoading = true;

            if (int.TryParse(Id, out int projectId))
            {
                var query = new GetTrainingStatisticsQuery { ProjectId = projectId };
                var result = await Mediator.Send(query);

                if (result.IsSuccess && result.Value != null)
                {
                    _statistics = result.Value;
                    PrepareGraphData();
                }
            }

            _isLoading = false;
        }

        private void PrepareGraphData()
        {
            if (_statistics == null) return;

            _classDistributionData = _statistics.ClassStatistics
                .ToDictionary(stat => stat.ClassName, stat => stat.TotalCount);

            _classColors = _statistics.ClassStatistics
                .Select(s => s.Color)
                .ToList();

            _splitDistributionData = new Dictionary<string, int>
            {
                { "Train", _statistics.TrainCount },
                { "Validation", _statistics.ValidationCount },
                { "Test", _statistics.TestCount }
            };
        }

        private async Task StartTraining()
        {
            if (_statistics == null || string.IsNullOrWhiteSpace(_trainingName))
            {
                await JSRuntime.InvokeVoidAsync("alert", "Please enter a training name.");
                return;
            }

            if (!_statistics.Validation.CanStartTraining)
            {
                await JSRuntime.InvokeVoidAsync("alert", "Training cannot be started. Please resolve all validation warnings first.");
                return;
            }

            _isCreatingTraining = true;
            StateHasChanged();

            try
            {
                var result = await TrainingCoordinator.CreateTrainingAsync(int.Parse(Id!), _trainingName);

                if (result.IsSuccess)
                {
                    // Navigate to main page after successful training creation
                    NavigationManager.NavigateTo("/wheeldl");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("alert", $"Failed to create training: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Error creating training: {ex.Message}");
            }
            finally
            {
                _isCreatingTraining = false;
                StateHasChanged();
            }
        }

        private void NavigateToPreviousPage()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                NavigationManager.NavigateTo($"/wheeldl/project/{Id}");
            }
            else
            {
                NavigationManager.NavigateTo("/wheeldl/datasets");
            }
        }

        private bool IsDetectionOrSegmentation()
        {
            return _statistics?.ProjectType == 1 || _statistics?.ProjectType == 2;
        }

        private string GetLabeledImagesLabel()
        {
            return IsDetectionOrSegmentation() ? "Annotated Images" : "Labeled Images";
        }

        private string GetClassDistributionTitle()
        {
            return IsDetectionOrSegmentation() ? "Annotation Distribution" : "Class Distribution";
        }

        private string GetAverageAnnotationsPerImage()
        {
            if (_statistics == null || _statistics.LabeledImageCount == 0) return "0.0";
            double avg = (double)_statistics.TotalAnnotationCount / _statistics.LabeledImageCount;
            return avg.ToString("F1");
        }

        private string GetTrainingNamePlaceholder()
        {
            if (_statistics == null) return "e.g., MyDataset_Training_v1";

            var datasetNameClean = _statistics.DatasetName.Replace(" ", "");
            var taskTypeClean = _statistics.TaskType.Replace(" ", "");
            return $"e.g., {datasetNameClean}_{taskTypeClean}_v1";
        }
    }
}
