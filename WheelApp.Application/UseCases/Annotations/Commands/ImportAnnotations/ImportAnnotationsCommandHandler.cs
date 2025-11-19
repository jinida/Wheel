using System.Text.Json;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace WheelApp.Application.UseCases.Annotations.Commands.ImportAnnotations;

/// <summary>
/// Handler for importing annotations from JSON
/// </summary>
public class ImportAnnotationsCommandHandler : ICommandHandler<ImportAnnotationsCommand, Result<ImportAnnotationResultDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectClassRepository _classRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<ImportAnnotationsCommandHandler> _logger;

    public ImportAnnotationsCommandHandler(
        IProjectRepository projectRepository,
        IProjectClassRepository classRepository,
        IImageRepository imageRepository,
        IAnnotationRepository annotationRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<ImportAnnotationsCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _classRepository = classRepository;
        _imageRepository = imageRepository;
        _annotationRepository = annotationRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result<ImportAnnotationResultDto>> Handle(ImportAnnotationsCommand request, CancellationToken cancellationToken)
    {
        var result = new ImportAnnotationResultDto();

        try
        {
            // Parse JSON content
            ImportAnnotationDto? importData;
            try
            {
                importData = JsonSerializer.Deserialize<ImportAnnotationDto>(request.JsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (importData == null || importData.Annotations == null || !importData.Annotations.Any())
                {
                    return Result.Failure<ImportAnnotationResultDto>("Invalid or empty JSON data.");
                }
            }
            catch (JsonException ex)
            {
                return Result.Failure<ImportAnnotationResultDto>($"Failed to parse JSON: {ex.Message}");
            }

            // Validate header type exists
            if (importData.Header?.Type == null || string.IsNullOrEmpty(importData.Header.Type))
            {
                return Result.Failure<ImportAnnotationResultDto>("Task type is missing in the JSON header.");
            }

            // Fetch project
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);

            if (project == null)
            {
                return Result.Failure<ImportAnnotationResultDto>($"Project with ID {request.ProjectId} not found.");
            }

            // Validate task type matches
            var projectTypeString = GetProjectTypeString(project.Type?.Value ?? -1);
            if (projectTypeString != importData.Header.Type)
            {
                return Result.Failure<ImportAnnotationResultDto>(
                    $"Task type mismatch: Project type is '{projectTypeString}', but JSON type is '{importData.Header.Type}'.");
            }

            // Detach the project entity to avoid tracking issues
            _projectRepository.Detach(project);

            // Fetch related data (keep them tracked for the transaction)
            var projectClasses = (await _classRepository.GetByProjectIdAsync(project.Id)).ToList();
            var images = (await _imageRepository.GetByDatasetIdAsync(project.DatasetId, cancellationToken)).ToList();

            // Don't detach anything - we need everything tracked for proper change detection

            // Validate and map categories, creating new ones if needed
            var categoryMap = new Dictionary<int, int>(); // Import index to ProjectClass ID
            if (importData.Header?.Categories == null || !importData.Header.Categories.Any())
            {
                return Result.Failure<ImportAnnotationResultDto>("Categories are missing in the JSON header.");
            }

            // Create a map of existing class names and class indices to their ProjectClass objects
            var existingClassByName = projectClasses.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
            var existingClassByIdx = projectClasses.ToDictionary(c => (int)c.ClassIdx, c => c);
            var classesToUpdate = new List<Domain.Entities.ProjectClass>();
            var importIdxToClass = new Dictionary<int, Domain.Entities.ProjectClass>(); // Track which class maps to which import index

            for (int importIdx = 0; importIdx < importData.Header.Categories.Count; importIdx++)
            {
                var categoryName = importData.Header.Categories[importIdx];

                // Check if a class with this name already exists
                if (existingClassByName.TryGetValue(categoryName, out var existingClass))
                {
                    // Same name exists
                    if (existingClass.ClassIdx == importIdx)
                    {
                        // Same name + same index: no change needed
                        importIdxToClass[importIdx] = existingClass;
                        _logger.LogInformation("Class '{ClassName}' with index {ImportIdx} already exists. Using existing class.",
                            categoryName, importIdx);
                    }
                    else
                    {
                        // Same name + different index: update the index
                        _logger.LogInformation("Updating class '{ClassName}' index from {OldIdx} to {NewIdx}",
                            categoryName, existingClass.ClassIdx, importIdx);

                        // Remove from old index map
                        existingClassByIdx.Remove((int)existingClass.ClassIdx);

                        // Update the class index
                        existingClass.UpdateClassIdx(importIdx);
                        classesToUpdate.Add(existingClass);

                        // Add to new index map
                        existingClassByIdx[importIdx] = existingClass;
                        importIdxToClass[importIdx] = existingClass;

                        result.Messages.Add($"Updated class '{categoryName}' index from {existingClass.ClassIdx} to {importIdx}");
                    }
                }
                else
                {
                    // Different name: create new class
                    // If the index is already taken, find next available index
                    int targetIdx = importIdx;
                    if (existingClassByIdx.ContainsKey(targetIdx))
                    {
                        // Find next available index
                        targetIdx = 0;
                        while (existingClassByIdx.ContainsKey(targetIdx))
                        {
                            targetIdx++;
                        }
                        _logger.LogWarning("Import index {ImportIdx} already taken by '{ExistingName}'. Creating '{NewName}' with index {NewIdx}",
                            importIdx, existingClassByIdx[importIdx].Name, categoryName, targetIdx);
                        result.Messages.Add($"Index {importIdx} already used. Created '{categoryName}' with index {targetIdx}");
                    }

                    var newClass = Domain.Entities.ProjectClass.Create(
                        project.Id,
                        targetIdx,
                        categoryName,
                        GenerateRandomColor());

                    await _classRepository.AddAsync(newClass, cancellationToken);

                    projectClasses.Add(newClass);
                    existingClassByName[categoryName] = newClass;
                    existingClassByIdx[targetIdx] = newClass;
                    importIdxToClass[importIdx] = newClass;

                    _logger.LogInformation("Created new class: '{ClassName}' with index {ActualIdx}",
                        categoryName, targetIdx);
                    result.Messages.Add($"Created new class: '{categoryName}' with index {targetIdx}");
                }
            }

            // No need to call UpdateAsync - EF Core is already tracking these entities
            // The changes made via UpdateClassIdx() will be automatically detected and saved

            // Build categoryMap with actual IDs (existing classes already have IDs)
            foreach (var kvp in importIdxToClass)
            {
                categoryMap[kvp.Key] = kvp.Value.Id;
            }

            // Don't detach classes yet - we might need them tracked for the transaction

            // Create a map of image names to Image entities for quick lookup
            var imageMap = images.ToDictionary(i => i.Name, i => i, StringComparer.OrdinalIgnoreCase);

            // Log categoryMap for debugging
            _logger.LogInformation("CategoryMap created with {Count} entries: {CategoryMap}",
                categoryMap.Count,
                string.Join(", ", categoryMap.Select(kvp => $"[{kvp.Key}]={kvp.Value}")));

            // Collect all annotations first, then add them in bulk
            _logger.LogInformation("Processing {Count} annotations from import data", importData.Annotations.Count);
            var allAnnotations = new List<Annotation>();

            foreach (var importItem in importData.Annotations)
            {
                try
                {
                    // Find the corresponding image
                    if (!imageMap.TryGetValue(importItem.Filename, out var image))
                    {
                        result.FailedCount++;
                        result.FailedItems.Add(importItem.Filename);
                        result.Messages.Add($"Image '{importItem.Filename}' not found in project.");
                        continue;
                    }

                    // Update role if provided and different from existing
                    if (importItem.Role >= 0 && importItem.Role <= 2)
                    {
                        await UpdateImageRole(image.Id, project.Id, importItem.Role, cancellationToken);
                    }

                    // Process annotations based on label type - collect them
                    var annotations = ProcessAnnotationLabelToList(
                        project,
                        image,
                        importItem.Label,
                        categoryMap);

                    if (annotations.Any())
                    {
                        allAnnotations.AddRange(annotations);
                        result.ImportedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                        result.Messages.Add($"No valid annotations found for image '{importItem.Filename}'.");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedItems.Add(importItem.Filename);
                    result.Messages.Add($"Error processing '{importItem.Filename}': {ex.Message}");
                    _logger.LogError(ex, "Error processing annotation for image '{Filename}'", importItem.Filename);
                }
            }

            // Add all annotations at once using AddRangeAsync
            if (allAnnotations.Any())
            {
                _logger.LogInformation("Adding {Count} annotations using AddRangeAsync", allAnnotations.Count);
                await _annotationRepository.AddRangeAsync(allAnnotations, cancellationToken);
            }

            // Save all changes
            // Changes are saved automatically by TransactionBehavior

            _logger.LogInformation("Import summary: Imported={Imported}, Skipped={Skipped}, Failed={Failed}",
                result.ImportedCount, result.SkippedCount, result.FailedCount);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportAnnotationResultDto>($"Import failed: {ex.Message}");
        }
    }

    private List<Annotation> ProcessAnnotationLabelToList(
        Project project,
        Image image,
        JsonElement labelElement,
        Dictionary<int, int> categoryMap)
    {
        var annotations = new List<Annotation>();

        try
        {
            // Classification (Type 0): single class index
            if (project.Type?.Value == 0)
            {
                if (labelElement.ValueKind == JsonValueKind.Number)
                {
                    var classIndex = labelElement.GetInt32();

                    // Validate class index is within bounds
                    if (classIndex < 0 || classIndex >= categoryMap.Count)
                    {
                        _logger.LogWarning("Invalid class index {ClassIndex} for image {ImageName}: Categories count is {Count}",
                            classIndex, image.Name, categoryMap.Count);
                        return annotations;
                    }

                    if (categoryMap.TryGetValue(classIndex, out var classId))
                    {
                        // Validate classId is positive
                        if (classId <= 0)
                        {
                            _logger.LogError("Invalid classId {ClassId} for class index {ClassIndex} in classification for image {ImageName}",
                                classId, classIndex, image.Name);
                            return annotations;
                        }

                        var annotation = Annotation.Create(
                            imageId: image.Id,
                            projectId: project.Id,
                            classId: classId,
                            information: null);

                        annotations.Add(annotation);
                    }
                    else
                    {
                        _logger.LogWarning("Class index {ClassIndex} not found in category map for image {ImageName}",
                            classIndex, image.Name);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid label format for classification task: expected number, got {ValueKind}",
                        labelElement.ValueKind);
                }
            }
            // Anomaly Detection (Type 3): 0 or 1
            else if (project.Type?.Value == 3)
            {
                if (labelElement.ValueKind == JsonValueKind.Number)
                {
                    var anomalyValue = labelElement.GetInt32();

                    // Validate anomaly value is 0 or 1
                    if (anomalyValue != 0 && anomalyValue != 1)
                    {
                        _logger.LogWarning("Invalid anomaly value {Value} for image {ImageName}: expected 0 or 1",
                            anomalyValue, image.Name);
                        return annotations;
                    }

                    // Validate class index exists
                    if (anomalyValue >= categoryMap.Count)
                    {
                        _logger.LogWarning("Anomaly value {Value} exceeds category count {Count} for image {ImageName}",
                            anomalyValue, categoryMap.Count, image.Name);
                        return annotations;
                    }

                    // Map 0 to "normal" class (index 0), 1 to "anomaly/defect" class (index 1)
                    if (categoryMap.TryGetValue(anomalyValue, out var classId))
                    {
                        // Validate classId is positive
                        if (classId <= 0)
                        {
                            _logger.LogError("Invalid classId {ClassId} for anomaly value {AnomalyValue} in image {ImageName}",
                                classId, anomalyValue, image.Name);
                            return annotations;
                        }

                        var annotation = Annotation.Create(
                            imageId: image.Id,
                            projectId: project.Id,
                            classId: classId,
                            information: null);

                        annotations.Add(annotation);
                    }
                    else
                    {
                        _logger.LogWarning("Anomaly value {Value} not found in category map for image {ImageName}",
                            anomalyValue, image.Name);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid label format for anomaly detection task: expected number, got {ValueKind}",
                        labelElement.ValueKind);
                }
            }
            // Object Detection (Type 1) or Segmentation (Type 2): array of annotations
            else if (project.Type?.Value == 1 || project.Type?.Value == 2)
            {
                if (labelElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var annotationElement in labelElement.EnumerateArray())
                    {
                        if (annotationElement.ValueKind == JsonValueKind.Array)
                        {
                            var elements = annotationElement.EnumerateArray().ToList();

                            if (elements.Count > 0 && elements[0].ValueKind == JsonValueKind.Number)
                            {
                                var classIndex = elements[0].GetInt32();

                                // Validate class index is within bounds
                                if (classIndex < 0 || classIndex >= categoryMap.Count)
                                {
                                    _logger.LogWarning("Invalid class index {ClassIndex} in annotation for image {ImageName}: Categories count is {Count}",
                                        classIndex, image.Name, categoryMap.Count);
                                    continue;
                                }

                                if (!categoryMap.TryGetValue(classIndex, out var classId))
                                {
                                    _logger.LogWarning("Class index {ClassIndex} not found in category map for image {ImageName}",
                                        classIndex, image.Name);
                                    continue;
                                }

                                // Validate classId is positive (required by Annotation.Create)
                                if (classId <= 0)
                                {
                                    _logger.LogError("Invalid classId {ClassId} for class index {ClassIndex} in image {ImageName}. CategoryMap: {CategoryMap}",
                                        classId, classIndex, image.Name, string.Join(", ", categoryMap.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                                    continue;
                                }

                                string informationJson;

                                // Detection (Type 1): Format is [cls, x1, y1, x2, y2] - Convert to [[x1,y1],[x2,y2]]
                                if (project.Type?.Value == 1)
                                {
                                    // Need exactly 5 elements: [classIdx, x1, y1, x2, y2]
                                    if (elements.Count == 5)
                                    {
                                        var x1 = (int)elements[1].GetDouble();
                                        var y1 = (int)elements[2].GetDouble();
                                        var x2 = (int)elements[3].GetDouble();
                                        var y2 = (int)elements[4].GetDouble();

                                        // Validate coordinates
                                        if (x2 <= x1 || y2 <= y1)
                                        {
                                            _logger.LogWarning("Invalid bounding box coordinates for image {ImageName}: x2 must be > x1 and y2 must be > y1",
                                                image.Name);
                                            continue;
                                        }

                                        if (x1 < 0 || y1 < 0)
                                        {
                                            _logger.LogWarning("Invalid bounding box coordinates for image {ImageName}: coordinates must be non-negative",
                                                image.Name);
                                            continue;
                                        }

                                        informationJson = JsonSerializer.Serialize(new List<int[]>
                                        {
                                            new int[] { x1, y1 },
                                            new int[] { x2, y2 }
                                        });
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Invalid detection format for image {ImageName}: expected 5 elements, got {Count}",
                                            image.Name, elements.Count);
                                        continue;
                                    }
                                }
                                // Segmentation (Type 2): Format is [cls, x1, y1, x2, y2, ...] - Convert to [[[x1,y1],[x2,y2],...]]
                                else
                                {
                                    // Extract all coordinate pairs (skip first element which is class index)
                                    var points = new List<int[]>();
                                    for (int i = 1; i < elements.Count - 1; i += 2)
                                    {
                                        if (elements[i].ValueKind == JsonValueKind.Number &&
                                            elements[i + 1].ValueKind == JsonValueKind.Number)
                                        {
                                            var x = (int)elements[i].GetDouble();
                                            var y = (int)elements[i + 1].GetDouble();

                                            // Validate coordinates are non-negative
                                            if (x < 0 || y < 0)
                                            {
                                                _logger.LogWarning("Invalid polygon coordinates for image {ImageName}: coordinates must be non-negative",
                                                    image.Name);
                                                continue;
                                            }

                                            points.Add(new int[] { x, y });
                                        }
                                    }

                                    if (points.Count >= 3) // Polygon needs at least 3 points
                                    {
                                        informationJson = JsonSerializer.Serialize(new List<List<int[]>> { points });
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Invalid segmentation format for image {ImageName}: need at least 3 points, got {Count}",
                                            image.Name, points.Count);
                                        continue;
                                    }
                                }

                                var annotation = Annotation.Create(
                                    imageId: image.Id,
                                    projectId: project.Id,
                                    classId: classId,
                                    information: informationJson);

                                annotations.Add(annotation);
                            }
                            else
                            {
                                _logger.LogWarning("Invalid annotation format for image {ImageName}: first element must be a class index number",
                                    image.Name);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Invalid annotation format for image {ImageName}: expected array, got {ValueKind}",
                                image.Name, annotationElement.ValueKind);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid label format for detection/segmentation task: expected array, got {ValueKind}",
                        labelElement.ValueKind);
                }
            }
            else
            {
                _logger.LogWarning("Unsupported project type: {Type}", project.Type?.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing annotation for image {ImageName}", image.Name);
        }

        return annotations;
    }

    private async Task UpdateImageRole(int imageId, int projectId, int roleValue, CancellationToken cancellationToken)
    {
        // Check if a role already exists for this image and project
        var existingRole = await _roleRepository.GetByImageAndProjectAsync(imageId, projectId, cancellationToken);

        if (existingRole != null)
        {
            // Only update if the role value is different
            if (existingRole.RoleType.Value != roleValue)
            {
                // Detach existing role first to avoid tracking conflicts
                _roleRepository.Detach(existingRole);

                // Fetch it again for updating
                existingRole = await _roleRepository.GetByImageAndProjectAsync(imageId, projectId, cancellationToken);
                if (existingRole != null)
                {
                    existingRole.ChangeRole(roleValue);
                    await _roleRepository.UpdateAsync(existingRole, cancellationToken);
                }
            }
        }
        else
        {
            // Create new role
            var newRole = Role.Create(imageId, projectId, roleValue);
            await _roleRepository.AddAsync(newRole, cancellationToken);
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

    private string GenerateRandomColor()
    {
        var random = new Random();
        var r = random.Next(50, 256);
        var g = random.Next(50, 256);
        var b = random.Next(50, 256);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
