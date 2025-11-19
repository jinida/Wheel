using System.Text.Json;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace WheelApp.Application.UseCases.Annotations.Queries.ExportAnnotations;

/// <summary>
/// Handler for exporting annotations
/// </summary>
public class ExportAnnotationsQueryHandler : IQueryHandler<ExportAnnotationsQuery, Result<ExportAnnotationDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectClassRepository _classRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<ExportAnnotationsQueryHandler> _logger;

    public ExportAnnotationsQueryHandler(
        IProjectRepository projectRepository,
        IProjectClassRepository classRepository,
        IImageRepository imageRepository,
        IAnnotationRepository annotationRepository,
        IRoleRepository roleRepository,
        ILogger<ExportAnnotationsQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _classRepository = classRepository;
        _imageRepository = imageRepository;
        _annotationRepository = annotationRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result<ExportAnnotationDto>> Handle(ExportAnnotationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Fetch the project
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);

            if (project == null)
            {
                return Result.Failure<ExportAnnotationDto>($"Project with ID {request.ProjectId} not found.");
            }

            // Fetch related data
            var projectClasses = await _classRepository.GetByProjectIdAsync(project.Id);
            var images = await _imageRepository.GetByDatasetIdAsync(project.DatasetId, cancellationToken);
            var annotations = await _annotationRepository.GetByProjectIdAsync(project.Id);
            var roles = await _roleRepository.GetByProjectIdAsync(project.Id);

            // Group annotations by image
            var annotationsByImage = annotations.GroupBy(a => a.ImageId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create a map of image roles
            var rolesByImage = roles.ToDictionary(r => r.ImageId, r => r.RoleType);

            // Prepare the export data
            var exportDto = new ExportAnnotationDto();

            // Build header
            exportDto.Header = new ExportHeaderDto
            {
                Version = "1.0.0",
                Type = GetProjectTypeString(project.Type?.Value ?? 0),
                Creator = "WheelApp",
                Categories = projectClasses
                    .OrderBy(c => c.Id)
                    .Select(c => c.Name)
                    .ToList(),
                Description = "Dataset annotations"
            };

            // Build annotations list
            var annotationItems = new List<ExportAnnotationItemDto>();

            // Create category index map for quick lookup
            var categoryIndexMap = projectClasses
                .OrderBy(c => c.Id)
                .Select((c, index) => new { c.Id, Index = index })
                .ToDictionary(x => x.Id, x => x.Index);

            // Create class lookup for quick access
            var classLookup = projectClasses.ToDictionary(c => c.Id);

            foreach (var image in images.OrderBy(i => i.Name))
            {
                // Get role value for this image
                var roleValue = rolesByImage.ContainsKey(image.Id)
                    ? rolesByImage[image.Id].Value
                    : 0;
                object labelValue;

                // Get annotations for this image
                var imageAnnotations = annotationsByImage.ContainsKey(image.Id)
                    ? annotationsByImage[image.Id]
                    : new List<Domain.Entities.Annotation>();

                // Anomaly Detection: 0 (normal) or 1 (anomaly)
                if (project.Type?.Value == 3)
                {
                    var firstAnnotation = imageAnnotations.FirstOrDefault();
                    if (firstAnnotation != null)
                    {
                        // Anomaly Detection: Check if the class name indicates anomaly
                        // Assuming class with index 1 or name containing "defect"/"anomaly" is anomalous
                        if (classLookup.TryGetValue(firstAnnotation.ClassId, out var classInfo))
                        {
                            var className = classInfo.Name.ToLower();
                            labelValue = (className.Contains("defect") || className.Contains("anomaly") ||
                                         categoryIndexMap[firstAnnotation.ClassId] == 1) ? 1 : 0;
                        }
                        else
                        {
                            labelValue = 0;
                        }
                    }
                    else
                    {
                        labelValue = 0;
                    }
                }
                // Classification: single class index
                else if (project.Type?.Value == 0)
                {
                    var firstAnnotation = imageAnnotations.FirstOrDefault();
                    if (firstAnnotation != null && categoryIndexMap.ContainsKey(firstAnnotation.ClassId))
                    {
                        labelValue = categoryIndexMap[firstAnnotation.ClassId];
                    }
                    else
                    {
                        labelValue = 0;
                    }
                }
                // Object Detection or Segmentation: array of annotations with coordinates
                else
                {
                    var annotationsArray = new List<object[]>();

                    foreach (var annotation in imageAnnotations)
                    {
                        if (!categoryIndexMap.ContainsKey(annotation.ClassId))
                        {
                            _logger.LogWarning("ClassId {ClassId} not found in category map for annotation {AnnotationId}",
                                annotation.ClassId, annotation.Id);
                            continue;
                        }

                        var categoryIdx = categoryIndexMap[annotation.ClassId];
                        var coords = new List<object> { categoryIdx };

                        _logger.LogInformation("Processing annotation {AnnotationId}, ClassId: {ClassId}, Information: {Info}",
                            annotation.Id, annotation.ClassId, annotation.Information ?? "NULL");

                        // Parse the Information JSON if it exists
                        if (!string.IsNullOrEmpty(annotation.Information))
                        {
                            try
                            {
                                // Object Detection (Type 1): [[x1,y1],[x2,y2]] format (2 points for bbox)
                                if (project.Type?.Value == 1)
                                {
                                    var pointArrays = JsonSerializer.Deserialize<List<int[]>>(annotation.Information);
                                    if (pointArrays != null && pointArrays.Count >= 2)
                                    {
                                        _logger.LogInformation("Parsed {Count} points for bbox annotation {AnnotationId}", pointArrays.Count, annotation.Id);

                                        coords.Add(pointArrays[0][0]); // x1
                                        coords.Add(pointArrays[0][1]); // y1
                                        coords.Add(pointArrays[1][0]); // x2
                                        coords.Add(pointArrays[1][1]); // y2
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Invalid bbox format for annotation {AnnotationId}", annotation.Id);
                                    }
                                }
                                // Segmentation (Type 2): [[[x1,y1],[x2,y2],...]] format (polygon points)
                                else if (project.Type?.Value == 2)
                                {
                                    var polygons = JsonSerializer.Deserialize<List<List<int[]>>>(annotation.Information);
                                    if (polygons != null && polygons.Count > 0 && polygons[0].Count > 0)
                                    {
                                        _logger.LogInformation("Parsed {Count} polygons for segmentation annotation {AnnotationId}", polygons.Count, annotation.Id);

                                        // Use first polygon's points
                                        foreach (var point in polygons[0])
                                        {
                                            if (point.Length >= 2)
                                            {
                                                coords.Add(point[0]); // x
                                                coords.Add(point[1]); // y
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Invalid segmentation format for annotation {AnnotationId}", annotation.Id);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to parse annotation information for annotation {AnnotationId}. Info: {Info}",
                                    annotation.Id, annotation.Information);
                                // Skip malformed annotation data
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Empty Information field for annotation {AnnotationId}", annotation.Id);
                        }

                        // Only add if we have coordinates
                        if (coords.Count > 1)
                        {
                            _logger.LogInformation("Adding annotation with {Count} values: [{Values}]",
                                coords.Count, string.Join(", ", coords));
                            annotationsArray.Add(coords.ToArray());
                        }
                        else
                        {
                            _logger.LogWarning("Skipping annotation {AnnotationId} - no coordinates added", annotation.Id);
                        }
                    }

                    labelValue = annotationsArray.ToArray();
                }

                annotationItems.Add(new ExportAnnotationItemDto
                {
                    Filename = image.Name,
                    Label = labelValue,
                    Role = roleValue
                });
            }

            exportDto.Annotations = annotationItems;

            return Result.Success(exportDto);
        }
        catch (Exception ex)
        {
            return Result.Failure<ExportAnnotationDto>($"Error exporting annotations: {ex.Message}");
        }
    }

    private string GetProjectTypeString(int projectType)
    {
        return projectType switch
        {
            0 => "classification",
            1 => "object_detection",
            2 => "segmentation",
            3 => "anomaly_detection",
            _ => "unknown"
        };
    }
}