using FluentValidation;

namespace WheelApp.Application.UseCases.Roles.Commands.RandomSplitRoles;

/// <summary>
/// Validator for RandomSplitRolesCommand
/// </summary>
public class RandomSplitRolesCommandValidator : AbstractValidator<RandomSplitRolesCommand>
{
    public RandomSplitRolesCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .WithMessage("Project ID must be a positive integer");

        RuleFor(x => x.TrainRatio)
            .InclusiveBetween(0, 1)
            .WithMessage("Train ratio must be between 0 and 1");

        RuleFor(x => x.ValidationRatio)
            .InclusiveBetween(0, 1)
            .WithMessage("Validation ratio must be between 0 and 1");

        RuleFor(x => x.TestRatio)
            .InclusiveBetween(0, 1)
            .WithMessage("Test ratio must be between 0 and 1");

        RuleFor(x => x)
            .Must(x => Math.Abs((x.TrainRatio + x.ValidationRatio + x.TestRatio) - 1.0) < 0.001)
            .WithMessage("The sum of Train, Validation, and Test ratios must equal 1.0")
            .WithName("RatioSum");

        RuleFor(x => x)
            .Must(x => x.TrainRatio > 0 || x.ValidationRatio > 0 || x.TestRatio > 0)
            .WithMessage("At least one ratio must be greater than 0")
            .WithName("AtLeastOneRatio");
    }
}