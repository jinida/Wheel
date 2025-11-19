using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotationsByImageIds;

/// <summary>
/// Handles batch creation of annotations to avoid N+1 query problem
/// </summary>
public class CreateAnnotationsByImageIdsCommandHandler : ICommandHandler<CreateAnnotationsByImageIdsCommand, Result<CreateAnnotationsByImageIdsResult>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAnnotationsByImageIdsCommandHandler> _logger;

    public CreateAnnotationsByImageIdsCommandHandler(
        IAnnotationRepository annotationRepository,
        IImageRepository imageRepository,
        IProjectRepository projectRepository,
        IProjectClassRepository projectClassRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<CreateAnnotationsByImageIdsCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _imageRepository = imageRepository;
        _projectRepository = projectRepository;
        _projectClassRepository = projectClassRepository;
        _logger = logger;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateAnnotationsByImageIdsResult>> Handle(CreateAnnotationsByImageIdsCommand request, CancellationToken cancellationToken)
    {
        if (!request.Annotations.Any())
        {
            _logger.LogWarning("No annotations to create in batch");
            return Result.Failure<CreateAnnotationsByImageIdsResult>("No annotations provided");
        }

        _logger.LogInformation("Creating {Count} annotations in batch", request.Annotations.Count);

        // Get unique ImageIds, ProjectIds, ClassIds for validation
        var imageIds = request.Annotations.Select(a => a.ImageId).Distinct().ToList();
        var projectIds = request.Annotations.Select(a => a.ProjectId).Distinct().ToList();
        var classIds = request.Annotations.Select(a => a.ClassId).Distinct().ToList();

        List<int> existingImageIds;
        try
        {
            existingImageIds = await _imageRepository.GetExistingIdsAsync(imageIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image IDs");
            throw;
        }

        var missingImageIds = imageIds.Except(existingImageIds).ToList();
        if (missingImageIds.Any())
        {
            _logger.LogWarning("Images not found: {MissingIds}", string.Join(", ", missingImageIds));
            return Result.Failure<CreateAnnotationsByImageIdsResult>($"Images not found: {string.Join(", ", missingImageIds)}");
        }

        List<int> existingProjectIds;
        try
        {
            existingProjectIds = await _projectRepository.GetExistingIdsAsync(projectIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating project IDs");
            throw;
        }

        var missingProjectIds = projectIds.Except(existingProjectIds).ToList();
        if (missingProjectIds.Any())
        {
            _logger.LogWarning("Projects not found: {MissingIds}", string.Join(", ", missingProjectIds));
            return Result.Failure<CreateAnnotationsByImageIdsResult>($"Projects not found: {string.Join(", ", missingProjectIds)}");
        }

        // Step 3: Validate classes (wait for previous query to complete)
        IReadOnlyList<ProjectClass> projectClasses;
        try
        {
            projectClasses = await _projectClassRepository.GetByIdsAsync(classIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating class IDs");
            throw;
        }

        var projectClassDict = projectClasses.ToDictionary(pc => pc.Id);

        foreach (var annotation in request.Annotations)
        {
            if (!projectClassDict.TryGetValue(annotation.ClassId, out var projectClass))
            {
                _logger.LogWarning("Class {ClassId} not found", annotation.ClassId);
                return Result.Failure<CreateAnnotationsByImageIdsResult>($"Class with ID {annotation.ClassId} does not exist");
            }

            if (projectClass.ProjectId != annotation.ProjectId)
            {
                _logger.LogWarning("Class {ClassId} does not belong to project {ProjectId}", annotation.ClassId, annotation.ProjectId);
                return Result.Failure<CreateAnnotationsByImageIdsResult>($"Class with ID {annotation.ClassId} does not belong to project with ID {annotation.ProjectId}");
            }
        }

        // Create all annotation entities
        var annotations = new List<Annotation>();
        foreach (var req in request.Annotations)
        {
            // Serialize List<Point2f> to [[X1, Y1], [X2, Y2]] format JSON string
            string? informationJson = null;
            if (req.Information != null && req.Information.Any())
            {
                var coordinates = req.Information
                    .Select(p => new[] { p.X, p.Y })
                    .ToList();
                informationJson = System.Text.Json.JsonSerializer.Serialize(coordinates);
            }

            var annotation = Annotation.Create(
                req.ImageId,
                req.ProjectId,
                req.ClassId,
                informationJson);

            annotations.Add(annotation);
        }

        // Batch insert all annotations
        // TransactionBehavior will automatically call SaveChanges AFTER this handler returns
        await _annotationRepository.AddRangeAsync(annotations, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var annotationDtos = _mapper.Map<List<AnnotationDto>>(annotations);

        _logger.LogInformation("Successfully created {Count} annotations in batch", annotations.Count);

        var result = new CreateAnnotationsByImageIdsResult
        {
            TotalCreated = annotations.Count,
            AnnotationDtos = annotationDtos
        };

        return Result.Success(result);
    }
}
