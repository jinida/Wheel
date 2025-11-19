using AutoMapper;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Queries.GetAnnotationsByImage;

/// <summary>
/// Handles retrieving annotations for a specific image
/// </summary>
public class GetAnnotationsByImageQueryHandler : IQueryHandler<GetAnnotationsByImageQuery, Result<List<AnnotationDto>>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;

    public GetAnnotationsByImageQueryHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<AnnotationDto>>> Handle(GetAnnotationsByImageQuery request, CancellationToken cancellationToken)
    {
        // Get annotations by image ID using efficient repository method
        var annotations = await _annotationRepository.GetByImageIdAsync(request.ImageId, cancellationToken);

        // Map to DTOs
        var annotationDtos = _mapper.Map<List<AnnotationDto>>(annotations);

        return Result.Success(annotationDtos);
    }
}
