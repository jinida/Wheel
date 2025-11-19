using AutoMapper;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Queries.GetAnnotationsByProject;

/// <summary>
/// Handles retrieving annotations for a specific project
/// </summary>
public class GetAnnotationsByProjectQueryHandler : IQueryHandler<GetAnnotationsByProjectQuery, Result<List<AnnotationDto>>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;

    public GetAnnotationsByProjectQueryHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<AnnotationDto>>> Handle(GetAnnotationsByProjectQuery request, CancellationToken cancellationToken)
    {
        // Get annotations by project ID
        var annotations = await _annotationRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

        // Map to DTOs
        var annotationDtos = _mapper.Map<List<AnnotationDto>>(annotations);

        return Result.Success(annotationDtos);
    }
}
