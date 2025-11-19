using FluentValidation;

namespace WheelApp.Application.UseCases.Annotations.Commands.UpdateAnnotationsByImageIds;

/// <summary>
/// Validator for UpdateAnnotationsByImageIdsCommand
/// Ensures batch size limits and valid IDs
/// </summary>
public class UpdateAnnotationsByImageIdsCommandValidator : AbstractValidator<UpdateAnnotationsByImageIdsCommand>
{
    private const int MaxBatchSize = 1000;

    public UpdateAnnotationsByImageIdsCommandValidator()
    {
        RuleFor(x => x.ImageIds)
            .NotNull()
            .WithMessage("Image IDs list cannot be null")
            .NotEmpty()
            .WithMessage("At least one image ID must be provided")
            .Must(ids => ids.Count <= MaxBatchSize)
            .WithMessage($"Cannot update more than {MaxBatchSize} images at once");

        RuleForEach(x => x.ImageIds)
            .GreaterThan(0)
            .WithMessage("Each image ID must be greater than 0");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .WithMessage("Project ID must be greater than 0");

        // ClassId can be null (to clear annotations) or a positive value
        RuleFor(x => x.ClassId)
            .GreaterThan(0)
            .When(x => x.ClassId.HasValue)
            .WithMessage("Class ID must be greater than 0 when provided");
    }
}