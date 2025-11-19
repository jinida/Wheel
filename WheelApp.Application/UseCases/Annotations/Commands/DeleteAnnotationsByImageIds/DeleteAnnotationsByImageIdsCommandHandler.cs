using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotationsByImageIds;

/// <summary>
/// Handles the deletion of all annotations for specified image IDs
/// </summary>
public class DeleteAnnotationsByImageIdsCommandHandler : ICommandHandler<DeleteAnnotationsByImageIdsCommand, Result<int>>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly ILogger<DeleteAnnotationsByImageIdsCommandHandler> _logger;

    public DeleteAnnotationsByImageIdsCommandHandler(
        IAnnotationRepository annotationRepository,
        ILogger<DeleteAnnotationsByImageIdsCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(DeleteAnnotationsByImageIdsCommand request, CancellationToken cancellationToken)
    {
        if (!request.ImageIds.Any())
        {
            _logger.LogWarning("No image IDs provided for annotation deletion");
            return Result.Failure<int>("No image IDs provided");
        }

        _logger.LogInformation("Attempting to delete annotations for {Count} image(s)", request.ImageIds.Count);

        // Delete all annotations for the specified image IDs
        var deletedCount = await _annotationRepository.DeleteByImageIdsAsync(request.ImageIds, cancellationToken);

        if (deletedCount == 0)
        {
            _logger.LogInformation("No annotations found for the provided image IDs");
            return Result.Success(0);
        }

        // Changes are saved automatically by TransactionBehavior

        _logger.LogInformation("Successfully deleted {DeletedCount} annotation(s) for {ImageCount} image(s)",
            deletedCount, request.ImageIds.Count);

        return Result.Success(deletedCount);
    }
}
