using FluentValidation;

namespace WheelApp.Application.UseCases.Datasets.Commands.UpdateDataset;

/// <summary>
/// Validator for UpdateDatasetCommand
/// </summary>
public class UpdateDatasetCommandValidator : AbstractValidator<UpdateDatasetCommand>
{
    public UpdateDatasetCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(50).WithMessage("Name cannot exceed 50 characters.")
            // Allow Unicode letters (includes Korean, Chinese, Japanese, etc.), numbers, spaces, hyphens, and underscores
            .Matches(@"^[\p{L}\p{N}_\- ]+$").WithMessage("Name can only contain letters, numbers, spaces, hyphens, and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description cannot exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ModifiedBy)
            .NotEmpty().WithMessage("ModifiedBy is required.");
    }
}
