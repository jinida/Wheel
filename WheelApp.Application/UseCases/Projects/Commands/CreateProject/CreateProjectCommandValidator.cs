using FluentValidation;

namespace WheelApp.Application.UseCases.Projects.Commands.CreateProject;

/// <summary>
/// Validator for CreateProjectCommand
/// </summary>
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(50).WithMessage("Name cannot exceed 50 characters.");

        RuleFor(x => x.DatasetId)
            .GreaterThan(0).WithMessage("DatasetId must be greater than 0.");

        RuleFor(x => x.Type)
            .InclusiveBetween(0, 3).WithMessage("Type must be between 0 and 3 (0=Classification, 1=ObjectDetection, 2=Segmentation, 3=AnomalyDetection).");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description cannot exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
