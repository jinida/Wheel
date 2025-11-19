using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotationsClass;

/// <summary>
/// Handles batch update of annotation classes
/// </summary>
public class UpdateAnnotationsClassCommandHandler : ICommandHandler<UpdateAnnotationsClassCommand, Result<int>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly ILogger<UpdateAnnotationsClassCommandHandler> _logger;

    public UpdateAnnotationsClassCommandHandler(
        IAnnotationRepository annotationRepository,
        IProjectClassRepository projectClassRepository,
        ILogger<UpdateAnnotationsClassCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _projectClassRepository = projectClassRepository;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(UpdateAnnotationsClassCommand request, CancellationToken cancellationToken)
    {
        if (request.AnnotationIds == null || !request.AnnotationIds.Any())
        {
            return Result.Failure<int>("No annotation IDs provided.");
        }

        _logger.LogInformation("Updating class for {Count} annotations to ClassId {ClassId}",
            request.AnnotationIds.Count, request.ClassId);

        try
        {
            // Validate that the class exists
            var projectClass = await _projectClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
            if (projectClass == null)
            {
                return Result.Failure<int>($"Class with ID {request.ClassId} does not exist.");
            }

            int updatedCount = 0;

            // Update each annotation
            foreach (var annotationId in request.AnnotationIds)
            {
                var annotation = await _annotationRepository.GetByIdAsync(annotationId, cancellationToken);
                if (annotation == null)
                {
                    _logger.LogWarning("Annotation {AnnotationId} not found, skipping", annotationId);
                    continue;
                }

                // Update the class (no need to call UpdateAsync - TransactionBehavior handles SaveChanges)
                annotation.ChangeClass(request.ClassId);
                updatedCount++;
            }

            _logger.LogInformation("Successfully updated {UpdatedCount} annotations", updatedCount);
            return Result.Success(updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating annotations class: {ErrorMessage}", ex.Message);
            return Result.Failure<int>($"Failed to update annotations: {ex.Message}");
        }
    }
}
