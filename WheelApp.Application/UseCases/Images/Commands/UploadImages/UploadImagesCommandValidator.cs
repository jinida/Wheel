using FluentValidation;

namespace WheelApp.Application.UseCases.Images.Commands.UploadImages;

/// <summary>
/// Validator for UploadImagesCommand
/// </summary>
public class UploadImagesCommandValidator : AbstractValidator<UploadImagesCommand>
{
    private const int MaxFilesPerUpload = 10000;
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

    public UploadImagesCommandValidator()
    {
        RuleFor(x => x.DatasetId)
            .GreaterThan(0).WithMessage("DatasetId must be greater than 0.");

        RuleFor(x => x.Files)
            .NotNull().WithMessage("Files cannot be empty.")
            .NotEmpty().WithMessage("Files cannot be empty.")
            .Must(files => files == null || files.Count <= MaxFilesPerUpload).WithMessage($"Cannot upload more than {MaxFilesPerUpload} files at once.");

        RuleForEach(x => x.Files)
            .ChildRules(file =>
            {
                file.RuleFor(f => f.FileName)
                    .NotEmpty().WithMessage("File name is required.")
                    .Must(HasValidExtension).WithMessage($"File must have one of the following extensions: {string.Join(", ", AllowedExtensions)}");

                file.RuleFor(f => f.FileSize)
                    .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("File size cannot exceed 20MB.");
            });
    }

    private bool HasValidExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
