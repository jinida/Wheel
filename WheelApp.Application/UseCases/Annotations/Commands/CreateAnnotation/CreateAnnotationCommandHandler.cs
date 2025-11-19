using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotation;

/// <summary>
/// Handles the creation of a new annotation
/// </summary>
public class CreateAnnotationCommandHandler : ICommandHandler<CreateAnnotationCommand, Result<AnnotationDto>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAnnotationCommandHandler> _logger;

    public CreateAnnotationCommandHandler(
        IAnnotationRepository annotationRepository,
        IImageRepository imageRepository,
        IProjectRepository projectRepository,
        IProjectClassRepository projectClassRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<CreateAnnotationCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _imageRepository = imageRepository;
        _projectRepository = projectRepository;
        _projectClassRepository = projectClassRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AnnotationDto>> Handle(CreateAnnotationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating annotation for image {ImageId}, project {ProjectId}, class {ClassId}",
            request.ImageId, request.ProjectId, request.ClassId);

        // Validate image exists
        var imageExists = await _imageRepository.ExistsAsync(request.ImageId, cancellationToken);
        if (!imageExists)
        {
            _logger.LogWarning("Failed to create annotation. Image {ImageId} does not exist", request.ImageId);
            return Result.Failure<AnnotationDto>($"Image with ID {request.ImageId} does not exist.");
        }

        // Validate project exists
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            _logger.LogWarning("Failed to create annotation. Project {ProjectId} does not exist", request.ProjectId);
            return Result.Failure<AnnotationDto>($"Project with ID {request.ProjectId} does not exist.");
        }

        // Validate class exists and belongs to the project
        var projectClass = await _projectClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (projectClass == null)
        {
            _logger.LogWarning("Failed to create annotation. Class {ClassId} does not exist", request.ClassId);
            return Result.Failure<AnnotationDto>($"Class with ID {request.ClassId} does not exist.");
        }

        if (projectClass.ProjectId != request.ProjectId)
        {
            _logger.LogWarning("Failed to create annotation. Class {ClassId} does not belong to project {ProjectId}",
                request.ClassId, request.ProjectId);
            return Result.Failure<AnnotationDto>($"The class with ID {request.ClassId} does not belong to project with ID {request.ProjectId}.");
        }

        // Create domain entity using factory method
        var annotation = Annotation.Create(
            request.ImageId,
            request.ProjectId,
            request.ClassId,
            request.Information);

        var addedAnnotation = await _annotationRepository.AddAsync(annotation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        _logger.LogInformation("Successfully created annotation {AnnotationId} for image {ImageId}",
            addedAnnotation.Id, request.ImageId);

        // Map to DTO and return
        var annotationDto = _mapper.Map<AnnotationDto>(addedAnnotation);
        return Result.Success(annotationDto);
    }
}
