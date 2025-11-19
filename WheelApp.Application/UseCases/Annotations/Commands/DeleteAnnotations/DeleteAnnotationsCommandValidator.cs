using FluentValidation;

namespace WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotations;

/// <summary>
/// Validator for DeleteAnnotationsCommand
/// </summary>
public class DeleteAnnotationsCommandValidator : AbstractValidator<DeleteAnnotationsCommand>
{
    private const int MaxDeletionsPerBatch = 100000;

    public DeleteAnnotationsCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull().WithMessage("Annotation IDs cannot be null.")
            .NotEmpty().WithMessage("At least one annotation ID must be provided.")
            .Must(ids => ids == null || ids.Count <= MaxDeletionsPerBatch)
                .WithMessage($"Cannot delete more than {MaxDeletionsPerBatch} annotations at once.");

        RuleForEach(x => x.Ids)
            .GreaterThan(0).WithMessage("Annotation ID must be greater than 0.");
    }
}
