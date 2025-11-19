using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotations;

/// <summary>
/// Handles the bulk deletion of multiple annotations
/// Uses optimized batch delete to avoid N+1 queries
/// </summary>
public class DeleteAnnotationsCommandHandler : ICommandHandler<DeleteAnnotationsCommand, Result<int>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly ILogger<DeleteAnnotationsCommandHandler> _logger;

    public DeleteAnnotationsCommandHandler(
        IAnnotationRepository annotationRepository,
        ILogger<DeleteAnnotationsCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;        
        _logger = logger;
    }

    public async Task<Result<int>> Handle(DeleteAnnotationsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete {Count} annotation(s)", request.Ids.Count);

        // Use DeleteRangeAsync to avoid N+1 queries
        var deletedCount = await _annotationRepository.DeleteRangeAsync(request.Ids, cancellationToken);

        if (deletedCount == 0)
        {
            _logger.LogWarning("No annotations were found for deletion");
            return Result.Failure<int>("No annotations were found for the provided IDs.");
        }

        // Commit the transaction
        // Changes are saved automatically by TransactionBehavior

        _logger.LogInformation("Successfully deleted {DeletedCount} annotation(s)", deletedCount);

        // Warn if some annotations were not found
        if (deletedCount < request.Ids.Count)
        {
            var notFoundCount = request.Ids.Count - deletedCount;
            _logger.LogWarning("{NotFoundCount} annotation(s) were not found", notFoundCount);
        }

        return Result.Success(deletedCount);
    }
}
