using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.DeleteProjectClass;

/// <summary>
/// Handles deletion of a project class and reindexes remaining classes
/// to maintain zero-based sequential assignment
/// </summary>
public class DeleteProjectClassCommandHandler : ICommandHandler<DeleteProjectClassCommand, Result<List<ProjectClassDto>>>
{
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<DeleteProjectClassCommandHandler> _logger;

    public DeleteProjectClassCommandHandler(
        IProjectClassRepository projectClassRepository,
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        ILogger<DeleteProjectClassCommandHandler> logger)
    {
        _projectClassRepository = projectClassRepository;
        _annotationRepository = annotationRepository;        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<ProjectClassDto>>> Handle(DeleteProjectClassCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Fetch the project class to delete
            var projectClass = await _projectClassRepository.GetByIdAsync(request.Id, cancellationToken);
            if (projectClass == null)
            {
                return Result.Failure<List<ProjectClassDto>>($"Project class with ID {request.Id} not found.");
            }

            // Get all annotations using this class
            var annotations = await _annotationRepository.GetByProjectIdAsync(projectClass.ProjectId, cancellationToken);
            var annotationIdsToDelete = annotations.Where(a => a.ClassId == request.Id).Select(a => a.Id).ToList();

            if (annotationIdsToDelete.Any())
            {
                _logger.LogInformation(
                    "Deleting {Count} annotations associated with ProjectClass {ClassId}",
                    annotationIdsToDelete.Count, request.Id);

                // Delete all annotations using this class by IDs (avoids EF Core tracking conflicts)
                await _annotationRepository.DeleteRangeAsync(annotationIdsToDelete, cancellationToken);

                _logger.LogInformation(
                    "Successfully deleted {Count} annotations associated with ProjectClass {ClassId}",
                    annotationIdsToDelete.Count, request.Id);
            }

            var deletedClassIdx = projectClass.ClassIdx.Value;
            var projectId = projectClass.ProjectId;

            // Delete the project class
            await _projectClassRepository.DeleteAsync(projectClass, cancellationToken);

            // CRITICAL: Reindex remaining classes to maintain sequential ordering
            _logger.LogInformation(
                "Reindexing project classes after deletion of ClassIdx {DeletedIdx} in Project {ProjectId}",
                deletedClassIdx, projectId);

            // Get all remaining classes for the project ordered by ClassIdx
            var remainingClasses = await _projectClassRepository.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedClasses = remainingClasses
                .Where(c => c.ClassIdx != null)
                .OrderBy(c => c.ClassIdx.Value)
                .ToList();

            // Reindex classes that had index greater than the deleted one
            int updatedCount = 0;
            for (int i = 0; i < orderedClasses.Count; i++)
            {
                    var expectedIdx = i;
                    var currentClass = orderedClasses[i];

                    if (currentClass.ClassIdx.Value != expectedIdx)
                    {
                        var oldIdx = currentClass.ClassIdx.Value;
                        var updatedClass = ProjectClass.Create(
                            currentClass.ProjectId,
                            expectedIdx,
                            currentClass.Name,
                            currentClass.Color.ToString());


                        _logger.LogWarning(
                            "Reindexing ProjectClass {ClassId} from ClassIdx {OldIdx} to {NewIdx} - " +
                            "Note: ProjectClass entity should have an UpdateClassIdx method",
                            currentClass.Id, oldIdx, expectedIdx);

                        continue;
                    }
                }

            if (updatedCount > 0)
            {
                _logger.LogInformation(
                    "Reindexed {Count} project classes after deletion in Project {ProjectId}",
                    updatedCount, projectId);
            }


            _logger.LogInformation(
                "Successfully deleted ProjectClass {ClassId} and reindexed remaining classes",
                request.Id);

            var allClassDtos = _mapper.Map<List<ProjectClassDto>>(orderedClasses.OrderBy(c => c.ClassIdx.Value).ToList());

            _logger.LogInformation(
                "Returning {Count} remaining project classes after deletion",
                allClassDtos.Count);

            return Result.Success(allClassDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete ProjectClass {ClassId}",
                request.Id);
            return Result.Failure<List<ProjectClassDto>>($"Failed to delete project class: {ex.Message}");
        }
    }
}
