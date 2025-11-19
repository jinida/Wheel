using FluentValidation;

namespace WheelApp.Application.UseCases.Projects.Commands.InitializeProject;

/// <summary>
/// Validator for InitializeProjectCommand
/// </summary>
public class InitializeProjectCommandValidator : AbstractValidator<InitializeProjectCommand>
{
    public InitializeProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .WithMessage("Project ID must be greater than 0");
    }
}