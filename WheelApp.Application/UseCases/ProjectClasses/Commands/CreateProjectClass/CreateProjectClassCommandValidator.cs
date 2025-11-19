using FluentValidation;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.CreateProjectClass;

/// <summary>
/// Validator for CreateProjectClassCommand
/// Validates uniqueness constraints within project (business rules)
/// </summary>
public class CreateProjectClassCommandValidator : AbstractValidator<CreateProjectClassCommand>
{
    private readonly IProjectClassRepository _repository;
    private readonly IProjectRepository _projectRepository;

    public CreateProjectClassCommandValidator(
        IProjectClassRepository repository,
        IProjectRepository projectRepository)
    {
        _repository = repository;
        _projectRepository = projectRepository;

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("Project ID must be greater than 0.")
            .MustAsync(async (projectId, cancellation) =>
            {
                // Validate that project exists
                return await _projectRepository.ExistsAsync(projectId, cancellation);
            })
            .WithMessage("Project does not exist.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Class name is required.")
            .MaximumLength(50).WithMessage("Class name cannot exceed 50 characters.")
            .MustAsync(async (command, name, cancellation) =>
            {
                // Business rule: Name must be unique within the project
                var existingClasses = await _repository.GetByProjectIdAsync(command.ProjectId, cancellation);
                return !existingClasses.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            })
            .WithMessage("This name is already used by another class in this project. Please choose a different name.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be in hex format (#RRGGBB).")
            .MustAsync(async (command, color, cancellation) =>
            {
                // Business rule: Color must be unique within the project
                var existingClasses = await _repository.GetByProjectIdAsync(command.ProjectId, cancellation);
                return !existingClasses.Any(c => string.Equals(c.Color, color, StringComparison.OrdinalIgnoreCase));
            })
            .WithMessage("This color is already used by another class in this project. Please choose a different color.");
    }
}
