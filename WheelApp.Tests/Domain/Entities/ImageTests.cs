using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for Image entity - HIGH PRIORITY: Core image management functionality
/// </summary>
public class ImageTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_ValidImage_CreatesSuccessfully()
    {
        // Arrange
        var name = "test.jpg";
        var path = "images/test.jpg";
        var datasetId = 1;

        // Act
        var image = Image.Create(name, path, datasetId);

        // Assert
        image.Should().NotBeNull();
        image.Name.Should().Be(name);
        image.Path.Value.Should().Be(path);
        image.DatasetId.Should().Be(datasetId);
        image.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceName_ThrowsValidationException(string invalidName)
    {
        // Act
        var act = () => Image.Create(invalidName, "images/test.jpg", 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Image name*cannot be empty*");
    }

    [Fact]
    public void Create_NameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var longName = new string('A', 51);  // Max is 50

        // Act
        var act = () => Image.Create(longName, "images/test.jpg", 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Image name*cannot exceed 50 characters*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespacePath_ThrowsValidationException(string invalidPath)
    {
        // Act
        var act = () => Image.Create("test.jpg", invalidPath, 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*cannot be empty*");
    }

    [Fact]
    public void Create_PathExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var longPath = new string('A', 513);  // Max is 512

        // Act
        var act = () => Image.Create("test.jpg", longPath, 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*cannot exceed 512 characters*");
    }

    [Theory]
    [InlineData("images/../test.jpg")]
    [InlineData("images//test.jpg")]
    [InlineData("images\\\\test.jpg")]
    public void Create_PathWithInvalidSequences_ThrowsValidationException(string invalidPath)
    {
        // Act
        var act = () => Image.Create("test.jpg", invalidPath, 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*contains invalid sequences*");
    }

    [Theory]
    [InlineData("images/test<file>.jpg")]
    [InlineData("images/test>file.jpg")]
    [InlineData("images/test:file.jpg")]
    [InlineData("images/test\"file.jpg")]
    [InlineData("images/test|file.jpg")]
    [InlineData("images/test?file.jpg")]
    [InlineData("images/test*file.jpg")]
    public void Create_PathWithDangerousCharacters_ThrowsValidationException(string invalidPath)
    {
        // Act
        var act = () => Image.Create("test.jpg", invalidPath, 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*contains invalid characters*");
    }

    #endregion

    #region AddAnnotation Tests

    [Fact]
    public void AddAnnotation_ValidAnnotation_AddsSuccessfully()
    {
        // Arrange
        var image = Image.Create("test.jpg", "images/test.jpg", 1);
        var annotation = Annotation.Create(image.Id, 1, 1, "[[10, 20]]");

        // Act
        image.AddAnnotation(annotation);

        // Assert
        image.Annotations.Should().ContainSingle();
        image.Annotations.First().Should().Be(annotation);
    }

    [Fact]
    public void AddAnnotation_MultipleAnnotations_AddsAllSuccessfully()
    {
        // Arrange
        var image = Image.Create("test.jpg", "images/test.jpg", 1);
        var annotation1 = Annotation.Create(image.Id, 1, 1, "[[10, 20]]");
        var annotation2 = Annotation.Create(image.Id, 1, 2, "[[30, 40]]");

        // Act
        image.AddAnnotation(annotation1);
        image.AddAnnotation(annotation2);

        // Assert
        image.Annotations.Should().HaveCount(2);
        image.Annotations.Should().Contain(annotation1);
        image.Annotations.Should().Contain(annotation2);
    }

    [Fact]
    public void AddAnnotation_NullAnnotation_ThrowsValidationException()
    {
        // Arrange
        var image = Image.Create("test.jpg", "images/test.jpg", 1);

        // Act
        var act = () => image.AddAnnotation(null!);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Annotation*cannot be null*");
    }

    #endregion

    #region UpdateName Tests

    [Fact]
    public void UpdateName_ValidName_UpdatesSuccessfully()
    {
        // Arrange
        var image = Image.Create("old.jpg", "images/old.jpg", 1);
        var newName = "new.jpg";

        // Act
        image.UpdateName(newName);

        // Assert
        image.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_EmptyOrWhitespaceName_ThrowsValidationException(string invalidName)
    {
        // Arrange
        var image = Image.Create("test.jpg", "images/test.jpg", 1);

        // Act
        var act = () => image.UpdateName(invalidName);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Image name*cannot be empty*");
    }

    [Fact]
    public void UpdateName_NameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var image = Image.Create("test.jpg", "images/test.jpg", 1);
        var longName = new string('A', 51);  // Max is 50

        // Act
        var act = () => image.UpdateName(longName);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Image name*cannot exceed 50 characters*");
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void Annotations_InitiallyEmpty()
    {
        // Arrange & Act
        var image = Image.Create("test.jpg", "images/test.jpg", 1);

        // Assert
        image.Annotations.Should().BeEmpty();
    }

    [Fact]
    public void Annotations_ReturnsReadOnlyCollection()
    {
        // Arrange
        var image = Image.Create("test.jpg", "images/test.jpg", 1);

        // Act & Assert
        image.Annotations.Should().BeAssignableTo<IReadOnlyCollection<Annotation>>();
    }

    #endregion

    #region CreatedAt Tests

    [Fact]
    public void CreatedAt_SetsDuringCreation()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var image = Image.Create("test.jpg", "images/test.jpg", 1);

        // Assert
        var afterCreate = DateTime.UtcNow;
        image.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        image.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Name_ExactlyMaxLength_CreatesSuccessfully()
    {
        // Arrange
        var maxLengthName = new string('A', 50);

        // Act
        var image = Image.Create(maxLengthName, "images/test.jpg", 1);

        // Assert
        image.Name.Should().Be(maxLengthName);
    }

    [Fact]
    public void Path_ReturnsFilePathValueObject()
    {
        // Act
        var image = Image.Create("test.jpg", "images/test.jpg", 1);

        // Assert
        image.Path.Should().BeOfType<FilePath>();
        image.Path.Value.Should().Be("images/test.jpg");
    }

    [Fact]
    public void DatasetId_IsSetCorrectly()
    {
        // Arrange
        var expectedDatasetId = 42;

        // Act
        var image = Image.Create("test.jpg", "images/test.jpg", expectedDatasetId);

        // Assert
        image.DatasetId.Should().Be(expectedDatasetId);
    }

    [Theory]
    [InlineData("image.jpg")]
    [InlineData("my-image_001.png")]
    [InlineData("test image 123.jpeg")]
    [InlineData("file.with.dots.jpg")]
    public void Create_ValidImageNames_CreatesSuccessfully(string validName)
    {
        // Act
        var image = Image.Create(validName, "images/test.jpg", 1);

        // Assert
        image.Name.Should().Be(validName);
    }

    [Theory]
    [InlineData("images/dataset1/test.jpg")]
    [InlineData("data/train/image.png")]
    [InlineData("uploads/user123/photo.jpg")]
    public void Create_ValidPaths_CreatesSuccessfully(string validPath)
    {
        // Act
        var image = Image.Create("test.jpg", validPath, 1);

        // Assert
        image.Path.Value.Should().Be(validPath);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var image = Image.Create("test.jpg", "images/test.jpg", 1);

        // Act
        image.ClearDomainEvents();

        // Assert
        image.DomainEvents.Should().BeEmpty();
    }

    #endregion
}
