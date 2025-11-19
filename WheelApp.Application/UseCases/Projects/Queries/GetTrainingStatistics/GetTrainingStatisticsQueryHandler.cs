using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Projects.Queries.GetTrainingStatistics
{
    public class GetTrainingStatisticsQueryHandler :
        IQueryHandler<GetTrainingStatisticsQuery, Result<TrainingStatisticsDto>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IProjectClassRepository _classRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IAnnotationRepository _annotationRepository;
        private readonly ILogger<GetTrainingStatisticsQueryHandler> _logger;

        public GetTrainingStatisticsQueryHandler(
            IProjectRepository projectRepository,
            IDatasetRepository datasetRepository,
            IProjectClassRepository classRepository,
            IImageRepository imageRepository,
            IRoleRepository roleRepository,
            IAnnotationRepository annotationRepository,
            ILogger<GetTrainingStatisticsQueryHandler> logger)
        {
            _projectRepository = projectRepository;
            _datasetRepository = datasetRepository;
            _classRepository = classRepository;
            _imageRepository = imageRepository;
            _roleRepository = roleRepository;
            _annotationRepository = annotationRepository;
            _logger = logger;
        }

        public async Task<Result<TrainingStatisticsDto>> Handle(
            GetTrainingStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Loading training statistics for Project {ProjectId}", request.ProjectId);

                // Load project and dataset
                var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
                if (project == null)
                {
                    return Result.Failure<TrainingStatisticsDto>($"Project with ID {request.ProjectId} not found");
                }

                var dataset = await _datasetRepository.GetByIdAsync(project.DatasetId, cancellationToken);
                if (dataset == null)
                {
                    return Result.Failure<TrainingStatisticsDto>("Dataset not found");
                }

                // Load all required data
                var classes = await _classRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
                var images = await _imageRepository.GetByDatasetIdAsync(project.DatasetId, cancellationToken);
                var roles = await _roleRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
                var annotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

                // Calculate statistics
                var labeledImageIds = annotations.Select(a => a.ImageId).Distinct().ToHashSet();
                var trainImageIds = roles.Where(r => r.RoleType == Domain.ValueObjects.RoleType.Train)
                    .Select(r => r.ImageId).ToHashSet();

                var classStatistics = new List<ClassStatistic>();
                // Order by ClassIdx Value with null handling
                var orderedClasses = classes.OrderBy(c => c.ClassIdx?.Value ?? int.MaxValue).ToList();

                foreach (var cls in orderedClasses)
                {
                    var classAnnotations = annotations.Where(a => a.ClassId == cls.Id).ToList();
                    var trainAnnotations = classAnnotations.Where(a => trainImageIds.Contains(a.ImageId)).Count();
                    var totalCount = classAnnotations.Count;

                    double percentage = labeledImageIds.Count > 0
                        ? (totalCount / (double)labeledImageIds.Count) * 100
                        : 0;

                    classStatistics.Add(new ClassStatistic
                    {
                        ClassName = cls.Name,
                        Color = cls.Color,
                        TotalCount = totalCount,
                        TrainCount = trainAnnotations,
                        Percentage = percentage
                    });
                }

                var isDetectionOrSegmentation = project.Type == 1 || project.Type == 2;

                var statisticsDto = new TrainingStatisticsDto
                {
                    ProjectName = project.Name,
                    DatasetName = dataset.Name,
                    DatasetDescription = dataset.Description ?? "",
                    TaskType = GetTaskTypeName(project.Type),
                    ProjectType = project.Type,
                    TotalImageCount = images.Count,
                    LabeledImageCount = labeledImageIds.Count,
                    TotalAnnotationCount = isDetectionOrSegmentation ? annotations.Count : 0,
                    TrainCount = roles.Count(r => r.RoleType == Domain.ValueObjects.RoleType.Train),
                    ValidationCount = roles.Count(r => r.RoleType == Domain.ValueObjects.RoleType.Validation),
                    TestCount = roles.Count(r => r.RoleType == Domain.ValueObjects.RoleType.Test),
                    ClassStatistics = classStatistics,
                    Validation = ValidateTrainingRequirements(labeledImageIds.Count,
                        roles.Count(r => r.RoleType == Domain.ValueObjects.RoleType.Train),
                        roles.Count(r => r.RoleType == Domain.ValueObjects.RoleType.Validation),
                        roles.Count(r => r.RoleType == Domain.ValueObjects.RoleType.Test),
                        classStatistics,
                        project.Type)
                };

                _logger.LogInformation("Training statistics loaded successfully for Project {ProjectId}", request.ProjectId);
                return Result.Success(statisticsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading training statistics for Project {ProjectId}", request.ProjectId);
                return Result.Failure<TrainingStatisticsDto>($"Failed to load training statistics: {ex.Message}");
            }
        }

        private static string GetTaskTypeName(int projectType)
        {
            return projectType switch
            {
                0 => "Classification",
                1 => "Detection",
                2 => "Segmentation",
                3 => "Anomaly Detection",
                _ => "Unknown"
            };
        }

        private static ValidationResult ValidateTrainingRequirements(
            int labeledImageCount,
            int trainCount,
            int validationCount,
            int testCount,
            List<ClassStatistic> classStats,
            int projectType)
        {
            var warnings = new List<string>();
            var canStartTraining = true;

            // Rule 1: Need at least 20 labeled images
            if (labeledImageCount < 20)
            {
                warnings.Add($"Insufficient labeled images: {labeledImageCount}/20 minimum required");
                canStartTraining = false;
            }

            // Rule 2: Need at least 15 images assigned to training
            if (trainCount < 15)
            {
                warnings.Add($"Insufficient training images: {trainCount}/15 minimum required");
                canStartTraining = false;
            }

            // Rule 3: Need at least 3 training images per class
            foreach (var classStat in classStats)
            {
                if (classStat.TrainCount < 3)
                {
                    warnings.Add($"Class '{classStat.ClassName}' has only {classStat.TrainCount} training images (3 minimum required)");
                    canStartTraining = false;
                }
            }

            // Rule 4: Insufficient evaluation set
            int evaluationCount = validationCount;
            if (evaluationCount == 0)
            {
                warnings.Add("No evaluation set: need at least validation or test images for model evaluation");
                canStartTraining = false;
            }
            else if (evaluationCount < 5)
            {
                warnings.Add($"Evaluation set is very small ({evaluationCount} images total): at least 5 images recommended");
                canStartTraining = false;
            }

            // Rule 5: Class imbalance
            if (classStats.Count > 1 && classStats.Any(c => c.TrainCount > 0))
            {
                var maxTrainCount = classStats.Max(c => c.TrainCount);
                var minTrainCount = classStats.Where(c => c.TrainCount > 0).Min(c => c.TrainCount);

                if (maxTrainCount > 0 && minTrainCount > 0)
                {
                    double imbalanceRatio = (double)maxTrainCount / minTrainCount;
                    if (imbalanceRatio > 10)
                    {
                        warnings.Add($"Severe class imbalance detected: ratio {imbalanceRatio:F1}:1 may affect training quality");
                    }
                }
            }

            // Rule 6: Classes with no training samples
            var classesWithNoTrainSamples = classStats.Where(c => c.TrainCount == 0).ToList();
            if (classesWithNoTrainSamples.Any())
            {
                var classNames = string.Join(", ", classesWithNoTrainSamples.Select(c => $"'{c.ClassName}'"));
                warnings.Add($"Classes with no training samples: {classNames}");
                canStartTraining = false;
            }

            // Rule 7: Classification needs at least 2 classes
            if (classStats.Count < 2 && projectType == 0)
            {
                warnings.Add("Only one class defined: classification tasks require at least 2 classes");
                canStartTraining = false;
            }

            return new ValidationResult
            {
                CanStartTraining = canStartTraining,
                Warnings = warnings
            };
        }
    }
}
