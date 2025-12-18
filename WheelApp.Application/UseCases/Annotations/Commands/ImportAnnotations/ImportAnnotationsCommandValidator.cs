using FluentValidation;
using System.Text.Json;

namespace WheelApp.Application.UseCases.Annotations.Commands.ImportAnnotations;

/// <summary>
/// Validator for ImportAnnotationsCommand
/// </summary>
public class ImportAnnotationsCommandValidator : AbstractValidator<ImportAnnotationsCommand>
{
    private static readonly string[] AllowedProjectTypes = { "classification", "object_detection", "segmentation", "anomaly_detection" };

    public ImportAnnotationsCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId must be greater than 0.");

        RuleFor(x => x.JsonContent)
            .NotEmpty().WithMessage("JSON content cannot be empty.")
            .Must(BeValidJson).WithMessage("JSON content is not valid JSON format.")
            .Must(HaveValidStructure).WithMessage("JSON must contain 'header' and 'annotations' properties.")
            .Must(HaveValidProjectType).WithMessage($"Header 'type' must be one of: {string.Join(", ", AllowedProjectTypes)}.")
            .Must(HaveCategories).WithMessage("Header 'categories' must not be empty.");
    }

    private bool BeValidJson(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool HaveValidStructure(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            return root.TryGetProperty("header", out _) &&
                   root.TryGetProperty("annotations", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool HaveValidProjectType(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.TryGetProperty("header", out var header) &&
                header.TryGetProperty("type", out var type) &&
                type.ValueKind == JsonValueKind.String)
            {
                var typeValue = type.GetString();
                return !string.IsNullOrEmpty(typeValue) && AllowedProjectTypes.Contains(typeValue.ToLowerInvariant());
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool HaveCategories(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.TryGetProperty("header", out var header) &&
                header.TryGetProperty("categories", out var categories) &&
                categories.ValueKind == JsonValueKind.Array)
            {
                return categories.GetArrayLength() > 0;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
