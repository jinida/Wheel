using FluentAssertions;
using WheelApp.Application.UseCases.Images.Commands.UploadImages;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Images.Commands;

/// <summary>
/// Tests for UploadImagesCommandValidator
/// CRITICAL: Tests security validation (file count, size, extensions)
/// </summary>
public class UploadImagesCommandValidatorTests
{
    private readonly UploadImagesCommandValidator _validator;

    public UploadImagesCommandValidatorTests()
    {
        _validator = new UploadImagesCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        // Arrange
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = "test.jpg", Stream = new MemoryStream(), FileSize = 1024 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidDatasetId_Fails(int datasetId)
    {
        // Arrange
        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = "test.jpg", Stream = new MemoryStream(), FileSize = 1024 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.DatasetId));
    }

    [Fact]
    public void Validate_EmptyFileList_Fails()
    {
        // Arrange
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Files));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("empty"));
    }

    [Fact]
    public void Validate_NullFileList_Fails()
    {
        // Arrange
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = null!
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Files));
    }

    [Fact]
    public void Validate_MoreThan10000Files_Fails()
    {
        // Arrange - CRITICAL: Test max file count limit
        var files = Enumerable.Range(1, 10001)
            .Select(i => new FileUploadInfo
            {
                FileName = $"file{i}.jpg",
                Stream = new MemoryStream(),
                FileSize = 1024
            })
            .ToList();

        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = files
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(command.Files) &&
            e.ErrorMessage.Contains("10000"));
    }

    [Fact]
    public void Validate_Exactly10000Files_Passes()
    {
        // Arrange - Test boundary condition
        var files = Enumerable.Range(1, 10000)
            .Select(i => new FileUploadInfo
            {
                FileName = $"file{i}.jpg",
                Stream = new MemoryStream(),
                FileSize = 1024
            })
            .ToList();

        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = files
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".exe")]
    [InlineData(".dll")]
    [InlineData(".bat")]
    [InlineData(".sh")]
    [InlineData(".pdf")]
    [InlineData(".zip")]
    [InlineData(".rar")]
    public void Validate_DisallowedExtension_Fails(string extension)
    {
        // Arrange - CRITICAL: Security - test disallowed extensions
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = $"malicious{extension}", Stream = new MemoryStream(), FileSize = 1024 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("extension") ||
            e.ErrorMessage.Contains(".jpg") ||
            e.ErrorMessage.Contains(".png"));
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".bmp")]
    [InlineData(".gif")]
    [InlineData(".JPG")]
    [InlineData(".JPEG")]
    [InlineData(".PNG")]
    public void Validate_AllowedExtension_Passes(string extension)
    {
        // Arrange - Test all allowed extensions including case variations
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = $"test{extension}", Stream = new MemoryStream(), FileSize = 1024 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_FileSizeExceeds20MB_Fails()
    {
        // Arrange - CRITICAL: Test max file size
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new()
                {
                    FileName = "large.jpg",
                    Stream = new MemoryStream(),
                    FileSize = 21 * 1024 * 1024 // 21MB
                }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("20MB") || e.ErrorMessage.Contains("size"));
    }

    [Fact]
    public void Validate_FileSizeExactly20MB_Passes()
    {
        // Arrange - Test boundary condition
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new()
                {
                    FileName = "large.jpg",
                    Stream = new MemoryStream(),
                    FileSize = 20 * 1024 * 1024 // Exactly 20MB
                }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyFileName_Fails(string? fileName)
    {
        // Arrange
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = fileName!, Stream = new MemoryStream(), FileSize = 1024 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("name"));
    }

    [Fact]
    public void Validate_MultipleFiles_OneInvalid_Fails()
    {
        // Arrange - Test that one invalid file fails the entire validation
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = "valid1.jpg", Stream = new MemoryStream(), FileSize = 1024 },
                new() { FileName = "invalid.exe", Stream = new MemoryStream(), FileSize = 1024 },
                new() { FileName = "valid2.png", Stream = new MemoryStream(), FileSize = 2048 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("extension"));
    }

    [Fact]
    public void Validate_MultipleFiles_OneExceedsSize_Fails()
    {
        // Arrange
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = "small.jpg", Stream = new MemoryStream(), FileSize = 1024 },
                new() { FileName = "large.jpg", Stream = new MemoryStream(), FileSize = 25 * 1024 * 1024 },
                new() { FileName = "medium.png", Stream = new MemoryStream(), FileSize = 2048 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("size") || e.ErrorMessage.Contains("20MB"));
    }

    [Fact]
    public void Validate_FileWithNoExtension_Fails()
    {
        // Arrange - CRITICAL: Files without extensions should fail
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = "noextension", Stream = new MemoryStream(), FileSize = 1024 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("extension"));
    }

    [Fact]
    public void Validate_MultipleValidFiles_Passes()
    {
        // Arrange
        var command = new UploadImagesCommand
        {
            DatasetId = 1,
            ProjectId = 1,
            Files = new List<FileUploadInfo>
            {
                new() { FileName = "photo1.jpg", Stream = new MemoryStream(), FileSize = 1024 * 1024 },
                new() { FileName = "photo2.png", Stream = new MemoryStream(), FileSize = 2 * 1024 * 1024 },
                new() { FileName = "photo3.gif", Stream = new MemoryStream(), FileSize = 512 * 1024 },
                new() { FileName = "photo4.bmp", Stream = new MemoryStream(), FileSize = 5 * 1024 * 1024 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
