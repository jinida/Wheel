using FluentValidation;

namespace WheelApp.Application.UseCases.Roles.Commands.UpdateRoleByIds;

/// <summary>
/// Validator for UpdateRoleByIdsCommand
/// </summary>
public class UpdateRoleByIdsCommandValidator : AbstractValidator<UpdateRoleByIdsCommand>
{
    public UpdateRoleByIdsCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .WithMessage("Project ID must be greater than 0");

        RuleFor(x => x.ImageIds)
            .NotNull()
            .WithMessage("Image IDs list cannot be null")
            .NotEmpty()
            .WithMessage("At least one image ID must be provided")
            .Must(ids => ids != null && ids.All(id => id > 0))
            .WithMessage("All image IDs must be greater than 0");

        RuleFor(x => x.RoleValue)
            .Must(value => !value.HasValue || (value.Value >= 0 && value.Value <= 3))
            .WithMessage("Role value must be null (defaults to None) or between 0 and 3 (0: Train, 1: Validation, 2: Test, 3: None)");
    }
}