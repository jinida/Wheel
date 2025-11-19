using FluentValidation;

namespace WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotation;

/// <summary>
/// Validator for CreateAnnotationCommand
/// </summary>
public class CreateAnnotationCommandValidator : AbstractValidator<CreateAnnotationCommand>
{
    public CreateAnnotationCommandValidator()
    {
        RuleFor(x => x.ImageId)
            .GreaterThan(0).WithMessage("ImageId must be greater than 0.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId must be greater than 0.");

        RuleFor(x => x.ClassId)
            .GreaterThan(0).WithMessage("ClassId must be greater than 0.");
    }
}
