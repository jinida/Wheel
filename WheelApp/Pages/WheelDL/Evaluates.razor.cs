using Microsoft.AspNetCore.Components;
using WheelApp.State;

namespace WheelApp.Pages.WheelDL
{
    public partial class Evaluates : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private bool _isLoading = true;
        private List<EvaluationResult> _allEvaluationResults = new();
        private List<EvaluationResult> _evaluationResults = new();
        private EvaluationResult? _selectedResult;
        private Dictionary<string, int> _classDistributionData = new();
        private Dictionary<string, int> _splitDistributionData = new();
        private List<string> _classColors = new();

        private readonly PaginationState _paginationState = new(pageSize: 5);
        private readonly SelectionState<int> _selectionState = new();

        protected override async Task OnInitializedAsync()
        {
            _isLoading = true;
            await LoadMockEvaluations();
            UpdatePaginatedData();
            _isLoading = false;
        }

        private async Task LoadMockEvaluations()
        {
            // Simulate API call
            await Task.Delay(100);

            var now = DateTime.UtcNow;

            _allEvaluationResults = new List<EvaluationResult>
            {
                new EvaluationResult
                {
                    Id = 1,
                    TrainingName = "Wheel_Defect_Detection_v1",
                    ProjectName = "Wheel Quality Control",
                    DatasetName = "Wheel Defect Dataset 2024",
                    TaskType = "Anomaly Detection",
                    CreatedAt = now.AddHours(-5),
                    EndedAt = now.AddHours(-2.5),
                    Duration = TimeSpan.FromHours(2.5),
                    Status = "Completed",
                    Metrics = new PerformanceMetrics
                    {
                        Accuracy = 0.9856,
                        Precision = 0.9723,
                        Recall = 0.9891,
                        F1Score = 0.9806,
                        AUROC = 0.9932
                    }
                },
                new EvaluationResult
                {
                    Id = 2,
                    TrainingName = "Tire_Classification_ResNet50",
                    ProjectName = "Tire Type Classification",
                    DatasetName = "Tire Dataset v3",
                    TaskType = "Multi-Class Classification",
                    CreatedAt = now.AddDays(-1),
                    EndedAt = now.AddDays(-1).AddHours(4.2),
                    Duration = TimeSpan.FromHours(4.2),
                    Status = "Completed",
                    Metrics = new PerformanceMetrics
                    {
                        Accuracy = 0.9567,
                        Precision = 0.9412,
                        Recall = 0.9634,
                        F1Score = 0.9522,
                        AUROC = 0.9801
                    }
                },
                new EvaluationResult
                {
                    Id = 3,
                    TrainingName = "Surface_Anomaly_EfficientNet",
                    ProjectName = "Surface Inspection",
                    DatasetName = "Manufacturing Defects 2024",
                    TaskType = "Binary Classification",
                    CreatedAt = now.AddDays(-2),
                    EndedAt = now.AddDays(-2).AddHours(3.8),
                    Duration = TimeSpan.FromHours(3.8),
                    Status = "Completed",
                    Metrics = new PerformanceMetrics
                    {
                        Accuracy = 0.9723,
                        Precision = 0.9645,
                        Recall = 0.9801,
                        F1Score = 0.9722,
                        AUROC = 0.9889
                    }
                },
                new EvaluationResult
                {
                    Id = 4,
                    TrainingName = "Wheel_Alignment_Check_v2",
                    ProjectName = "Alignment Detection",
                    DatasetName = "Alignment Dataset",
                    TaskType = "Anomaly Detection",
                    CreatedAt = now.AddDays(-3),
                    EndedAt = now.AddDays(-3).AddHours(1.5),
                    Duration = TimeSpan.FromHours(1.5),
                    Status = "Failed",
                    Metrics = new PerformanceMetrics
                    {
                        Accuracy = 0.7834,
                        Precision = 0.7512,
                        Recall = 0.8023,
                        F1Score = 0.7760,
                        AUROC = 0.8456
                    }
                }
            };

            // Generate more mock data for pagination testing
            for (int i = 5; i <= 35; i++)
            {
                _allEvaluationResults.Add(new EvaluationResult
                {
                    Id = i,
                    TrainingName = $"Training_Model_{i}",
                    ProjectName = $"Project_{(i % 5) + 1}",
                    DatasetName = $"Dataset_{(i % 3) + 1}",
                    TaskType = i % 2 == 0 ? "Classification" : "Detection",
                    CreatedAt = now.AddDays(-i),
                    EndedAt = now.AddDays(-i).AddHours(2 + (i % 3)),
                    Duration = TimeSpan.FromHours(2 + (i % 3)),
                    Status = i % 7 == 0 ? "Failed" : "Completed",
                    Metrics = new PerformanceMetrics
                    {
                        Accuracy = 0.85 + (i % 15) * 0.01,
                        Precision = 0.83 + (i % 15) * 0.01,
                        Recall = 0.87 + (i % 15) * 0.01,
                        F1Score = 0.85 + (i % 15) * 0.01,
                        AUROC = 0.90 + (i % 10) * 0.01
                    }
                });
            }

            // Update pagination
            int totalPages = (int)Math.Ceiling(_allEvaluationResults.Count / (double)_paginationState.PageSize);
            _paginationState.UpdatePagination(_allEvaluationResults.Count, totalPages);
        }

        private void UpdatePaginatedData()
        {
            int skip = (_paginationState.CurrentPage - 1) * _paginationState.PageSize;
            _evaluationResults = _allEvaluationResults
                .Skip(skip)
                .Take(_paginationState.PageSize)
                .ToList();
        }

        private async Task HandlePageChange(int pageNumber)
        {
            _paginationState.GoToPage(pageNumber);
            UpdatePaginatedData();
            StateHasChanged();
            await Task.CompletedTask;
        }

        private void SelectResult(EvaluationResult result)
        {
            if (_selectedResult?.Id == result.Id)
            {
                _selectedResult = null;
            }
            else
            {
                _selectedResult = result;
                LoadMockChartData();
            }
        }

        private void LoadMockChartData()
        {
            // Mock class distribution data
            _classDistributionData = new Dictionary<string, int>
            {
                { "Normal", 4235 },
                { "Defect", 892 },
                { "Uncertain", 73 }
            };

            _classColors = new List<string>
            {
                "#10b981", // Green for Normal
                "#ef4444", // Red for Defect
                "#f59e0b"  // Orange for Uncertain
            };

            // Mock train/val/test split data
            _splitDistributionData = new Dictionary<string, int>
            {
                { "Train", 3640 },
                { "Val", 910 },
                { "Test", 650 }
            };
        }

        private void ToggleAllRows(ChangeEventArgs e)
        {
            var currentPageIds = _evaluationResults.Select(r => r.Id);
            _selectionState.ToggleAll(currentPageIds);
        }

        private bool IsResultSelected(int id)
        {
            return _selectionState.IsSelected(id);
        }

        private void ToggleResultSelection(int id)
        {
            _selectionState.Toggle(id);
        }

        private void NavigateToDetail()
        {
            if (_selectedResult != null)
            {
                NavigationManager.NavigateTo($"/wheeldl/evaluate/{_selectedResult.Id}");
            }
        }

        // Models
        public class EvaluationResult
        {
            public int Id { get; set; }
            public string TrainingName { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public string DatasetName { get; set; } = string.Empty;
            public string TaskType { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public TimeSpan Duration { get; set; }
            public string Status { get; set; } = string.Empty;
            public PerformanceMetrics Metrics { get; set; } = new();
        }

        public class PerformanceMetrics
        {
            public double Accuracy { get; set; }
            public double Precision { get; set; }
            public double Recall { get; set; }
            public double F1Score { get; set; }
            public double AUROC { get; set; }
        }
    }
}
