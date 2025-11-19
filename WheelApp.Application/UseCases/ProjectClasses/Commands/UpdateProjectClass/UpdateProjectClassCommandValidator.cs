using FluentValidation;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.UpdateProjectClass;

/// <summary>
/// Validator for UpdateProjectClassCommand
/// Validates uniqueness constraints within project (business rules)
/// </summary>
public class UpdateProjectClassCommandValidator : AbstractValidator<UpdateProjectClassCommand>
{
    private readonly IProjectClassRepository _repository;

    public UpdateProjectClassCommandValidator(IProjectClassRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID must be greater than 0.")
            .MustAsync(async (id, cancellation) =>
            {
                // Validate that project class exists
                var projectClass = await _repository.GetByIdAsync(id, cancellation);
                return projectClass != null;
            })
            .WithMessage("Project class does not exist.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Class name is required.")
            .MaximumLength(50).WithMessage("Class name cannot exceed 50 characters.")
            .MustAsync(async (command, name, cancellation) =>
            {
                // Business rule: Name must be unique within the project (excluding current class)
                var currentClass = await _repository.GetByIdAsync(command.Id, cancellation);
                if (currentClass == null)
                {
                    return true; // Will be caught by Id validation
                }

                var existingClasses = await _repository.GetByProjectIdAsync(currentClass.ProjectId, cancellation);
                return !existingClasses.Any(c =>
                    c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    c.Id != command.Id); // Exclude current class from check
            })
            .WithMessage("This name is already used by another class in this project. Please choose a different name.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be in hex format (#RRGGBB).")
            .MustAsync(async (command, color, cancellation) =>
            {
                // Business rule: Color must be unique within the project (excluding current class)
                var currentClass = await _repository.GetByIdAsync(command.Id, cancellation);
                if (currentClass == null)
                {
                    return true; // Will be caught by Id validation
                }

                var existingClasses = await _repository.GetByProjectIdAsync(currentClass.ProjectId, cancellation);
                return !existingClasses.Any(c =>
                    string.Equals(c.Color, color, StringComparison.OrdinalIgnoreCase) &&
                    c.Id != command.Id); // Exclude current class from check
            })
            .WithMessage("This color is already used by another class in this project. Please choose a different color.");
    }
}
