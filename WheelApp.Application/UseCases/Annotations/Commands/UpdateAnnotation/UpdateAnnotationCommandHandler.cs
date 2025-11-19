using Microsoft.Extensions.Logging;
using System.Text.Json;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotation;

/// <summary>
/// Handles updating a single annotation
/// </summary>
public class UpdateAnnotationCommandHandler : ICommandHandler<UpdateAnnotationCommand, Result>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly ILogger<UpdateAnnotationCommandHandler> _logger;

    public UpdateAnnotationCommandHandler(
        IAnnotationRepository annotationRepository,
        ILogger<UpdateAnnotationCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateAnnotationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update annotation ID: {AnnotationId}", request.Id);

        // Fetch the annotation (tracked by EF Core via FindAsync)
        var annotation = await _annotationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (annotation == null)
        {
            _logger.LogWarning("Annotation with ID {AnnotationId} not found", request.Id);
            return Result.Failure($"Annotation with ID {request.Id} not found.");
        }

        // Serialize Point2f list to JSON string
        string? informationJson = null;
        if (request.Information != null && request.Information.Any())
        {
            var coordinates = request.Information.Select(p => new[] { p.X, p.Y }).ToList();
            informationJson = JsonSerializer.Serialize(coordinates);
        }

        // Update both Information and ClassId (entity is tracked, changes will be saved by TransactionBehavior)
        annotation.UpdateAnnotation(informationJson);
        annotation.ChangeClass(request.ClassId);

        // No need to call SaveChanges - TransactionBehavior handles this

        _logger.LogInformation("Successfully updated annotation ID: {AnnotationId}", request.Id);

        return Result.Success();
    }
}
