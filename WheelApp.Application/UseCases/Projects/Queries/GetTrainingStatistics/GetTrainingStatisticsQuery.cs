using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;

namespace WheelApp.Application.UseCases.Projects.Queries.GetTrainingStatistics
{
    public class GetTrainingStatisticsQuery : IQuery<Result<TrainingStatisticsDto>>
    {
        public int ProjectId { get; set; }
    }

    public class TrainingStatisticsDto
    {
        public required string ProjectName { get; set; }
        public required string DatasetName { get; set; }
        public required string DatasetDescription { get; set; }
        public required string TaskType { get; set; }
        public int ProjectType { get; set; }
        public int TotalImageCount { get; set; }
        public int LabeledImageCount { get; set; }
        public int TotalAnnotationCount { get; set; }
        public int TrainCount { get; set; }
        public int ValidationCount { get; set; }
        public int TestCount { get; set; }
        public List<ClassStatistic> ClassStatistics { get; set; } = new();
        public ValidationResult Validation { get; set; } = new();
    }

    public class ClassStatistic
    {
        public required string ClassName { get; set; }
        public required string Color { get; set; }
        public int TotalCount { get; set; }
        public int TrainCount { get; set; }
        public double Percentage { get; set; }
    }

    public class ValidationResult
    {
        public bool CanStartTraining { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
