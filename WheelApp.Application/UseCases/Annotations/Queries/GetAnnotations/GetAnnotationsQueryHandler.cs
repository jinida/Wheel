using AutoMapper;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Queries.GetAnnotations;

/// <summary>
/// Handles retrieving annotations with filtering and paging
/// </summary>
public class GetAnnotationsQueryHandler : IQueryHandler<GetAnnotationsQuery, Result<PagedResult<AnnotationDto>>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;

    public GetAnnotationsQueryHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<AnnotationDto>>> Handle(GetAnnotationsQuery request, CancellationToken cancellationToken)
    {
        // Get annotations based on filters
        IReadOnlyList<Annotation> annotations;

        if (request.ProjectId.HasValue && request.ImageId.HasValue)
        {
            // Filter by both project and image
            var allAnnotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId.Value, cancellationToken);
            annotations = allAnnotations.Where(a => a.ImageId == request.ImageId.Value).ToList();
        }
        else if (request.ProjectId.HasValue)
        {
            // Filter by project only
            annotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId.Value, cancellationToken);
        }
        else if (request.ImageId.HasValue)
        {
            // Filter by image only
            var allAnnotations = await _annotationRepository.GetAllAsync(cancellationToken);
            annotations = allAnnotations.Where(a => a.ImageId == request.ImageId.Value).ToList();
        }
        else
        {
            // No filters - get all
            annotations = await _annotationRepository.GetAllAsync(cancellationToken);
        }

        // Get total count before paging
        var totalCount = annotations.Count;

        // Apply paging
        var pagedAnnotations = annotations
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Map to DTOs
        var annotationDtos = _mapper.Map<IReadOnlyList<AnnotationDto>>(pagedAnnotations);

        // Create paged result
        var pagedResult = PagedResult<AnnotationDto>.Create(
            annotationDtos,
            totalCount,
            request.PageNumber,
            request.PageSize
        );

        return Result.Success(pagedResult);
    }
}
