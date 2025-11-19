using AutoMapper;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.ProjectClasses.Queries.GetProjectClassesByProject;

/// <summary>
/// Handles retrieving all classes for a specific project
/// </summary>
public class GetProjectClassesByProjectQueryHandler : IQueryHandler<GetProjectClassesByProjectQuery, Result<List<ProjectClassDto>>>
{
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;

    public GetProjectClassesByProjectQueryHandler(
        IProjectClassRepository projectClassRepository,
        IAnnotationRepository annotationRepository,
        IMapper mapper)
    {
        _projectClassRepository = projectClassRepository;
        _annotationRepository = annotationRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<ProjectClassDto>>> Handle(GetProjectClassesByProjectQuery request, CancellationToken cancellationToken)
    {
        // Get classes for the project using repository method
        var projectClasses = await _projectClassRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

        // Order by class index
        var orderedClasses = projectClasses.OrderBy(c => c.ClassIdx.Value).ToList();

        // Map to DTOs
        var dtos = _mapper.Map<List<ProjectClassDto>>(orderedClasses);

        return Result.Success(dtos);
    }
}
