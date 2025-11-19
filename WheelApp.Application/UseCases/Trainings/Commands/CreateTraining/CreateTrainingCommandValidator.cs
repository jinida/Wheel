using FluentValidation;

namespace WheelApp.Application.UseCases.Trainings.Commands.CreateTraining;

/// <summary>
/// Validator for CreateTrainingCommand
/// </summary>
public class CreateTrainingCommandValidator : AbstractValidator<CreateTrainingCommand>
{
    public CreateTrainingCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .WithMessage("Project ID must be greater than 0.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Training name is required.")
            .MaximumLength(200)
            .WithMessage("Training name must not exceed 200 characters.");
    }
}
