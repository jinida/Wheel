using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Events.AnnotationEvents;
using WheelApp.Domain.Exceptions;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for Annotation entity - HIGH PRIORITY: Core labeling functionality
/// </summary>
public class AnnotationTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_ValidAnnotation_CreatesSuccessfully()
    {
        // Arrange
        var imageId = 1;
        var projectId = 2;
        var classId = 3;
        var information = "[[10.5, 20.3], [30.1, 40.2]]";

        // Act
        var annotation = Annotation.Create(imageId, projectId, classId, information);

        // Assert
        annotation.Should().NotBeNull();
        annotation.ImageId.Should().Be(imageId);
        annotation.ProjectId.Should().Be(projectId);
        annotation.ClassId.Should().Be(classId);
        annotation.Information.Should().Be(information);
        annotation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullInformation_CreatesSuccessfully()
    {
        // Arrange
        var imageId = 1;
        var projectId = 2;
        var classId = 3;

        // Act
        var annotation = Annotation.Create(imageId, projectId, classId, null);

        // Assert
        annotation.Should().NotBeNull();
        annotation.Information.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutInformationParameter_CreatesWithNullInformation()
    {
        // Act
        var annotation = Annotation.Create(1, 2, 3);

        // Assert
        annotation.Information.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_InvalidImageId_ThrowsValidationException(int invalidImageId)
    {
        // Act
        var act = () => Annotation.Create(invalidImageId, 1, 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Image ID*cannot be negative*");
    }

    [Fact]
    public void Create_ImageIdZero_AllowedForDomainTests()
    {
        // Domain tests create entities without DB persistence (Id=0)
        // Act
        var annotation = Annotation.Create(0, 1, 1);

        // Assert
        annotation.Should().NotBeNull();
        annotation.ImageId.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_InvalidProjectId_ThrowsValidationException(int invalidProjectId)
    {
        // Act
        var act = () => Annotation.Create(1, invalidProjectId, 1);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Project ID*must be positive*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_InvalidClassId_ThrowsValidationException(int invalidClassId)
    {
        // Act
        var act = () => Annotation.Create(1, 1, invalidClassId);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class ID*must be positive*");
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Create_RaisesAnnotationCreatedEvent()
    {
        // Act
        var annotation = Annotation.Create(1, 2, 3, "[[10, 20]]");

        // Assert
        var domainEvents = annotation.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AnnotationCreatedEvent>();
    }

    [Fact]
    public void UpdateAnnotation_RaisesAnnotationUpdatedEvent()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, "[[10, 20]]");
        annotation.ClearDomainEvents();
        var newInfo = "[[15, 25]]";

        // Act
        annotation.UpdateAnnotation(newInfo);

        // Assert
        var domainEvents = annotation.DomainEvents;
        var updatedEvent = domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AnnotationUpdatedEvent>().Subject;

        updatedEvent.OldInformation.Should().Be("[[10, 20]]");
        updatedEvent.NewInformation.Should().Be(newInfo);
    }

    [Fact]
    public void UpdateAnnotation_FromNullToValue_RaisesEventWithEmptyOldValue()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, null);
        annotation.ClearDomainEvents();

        // Act
        annotation.UpdateAnnotation("[[10, 20]]");

        // Assert
        var updatedEvent = annotation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AnnotationUpdatedEvent>().Subject;

        updatedEvent.OldInformation.Should().Be("");
        updatedEvent.NewInformation.Should().Be("[[10, 20]]");
    }

    [Fact]
    public void UpdateAnnotation_FromValueToNull_RaisesEventWithEmptyNewValue()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, "[[10, 20]]");
        annotation.ClearDomainEvents();

        // Act
        annotation.UpdateAnnotation(null);

        // Assert
        var updatedEvent = annotation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AnnotationUpdatedEvent>().Subject;

        updatedEvent.OldInformation.Should().Be("[[10, 20]]");
        updatedEvent.NewInformation.Should().Be("");
    }

    #endregion

    #region UpdateAnnotation Tests

    [Fact]
    public void UpdateAnnotation_ValidInformation_UpdatesSuccessfully()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, "[[10, 20]]");
        var newInfo = "[[15, 25], [30, 40]]";

        // Act
        annotation.UpdateAnnotation(newInfo);

        // Assert
        annotation.Information.Should().Be(newInfo);
    }

    [Fact]
    public void UpdateAnnotation_ToNull_UpdatesSuccessfully()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, "[[10, 20]]");

        // Act
        annotation.UpdateAnnotation(null);

        // Assert
        annotation.Information.Should().BeNull();
    }

    [Fact]
    public void UpdateAnnotation_FromNull_UpdatesSuccessfully()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, null);

        // Act
        annotation.UpdateAnnotation("[[10, 20]]");

        // Assert
        annotation.Information.Should().Be("[[10, 20]]");
    }

    #endregion

    #region ChangeClass Tests

    [Fact]
    public void ChangeClass_ValidClassId_ChangesSuccessfully()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, null);
        var newClassId = 5;

        // Act
        annotation.ChangeClass(newClassId);

        // Assert
        annotation.ClassId.Should().Be(newClassId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ChangeClass_InvalidClassId_ThrowsValidationException(int invalidClassId)
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, null);

        // Act
        var act = () => annotation.ChangeClass(invalidClassId);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class ID*must be positive*");
    }

    #endregion

    #region Navigation Properties Tests

    [Fact]
    public void Image_InitiallyNull()
    {
        // Arrange & Act
        var annotation = Annotation.Create(1, 2, 3, null);

        // Assert
        annotation.Image.Should().BeNull();
    }

    [Fact]
    public void Project_InitiallyNull()
    {
        // Arrange & Act
        var annotation = Annotation.Create(1, 2, 3, null);

        // Assert
        annotation.Project.Should().BeNull();
    }

    [Fact]
    public void ProjectClass_InitiallyNull()
    {
        // Arrange & Act
        var annotation = Annotation.Create(1, 2, 3, null);

        // Assert
        annotation.ProjectClass.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_LargeJsonInformation_CreatesSuccessfully()
    {
        // Arrange
        var largeInfo = "[[" + string.Join(", ", Enumerable.Range(1, 1000).Select(x => $"[{x}, {x * 2}]")) + "]]";

        // Act
        var annotation = Annotation.Create(1, 2, 3, largeInfo);

        // Assert
        annotation.Information.Should().Be(largeInfo);
    }

    [Fact]
    public void Create_EmptyStringInformation_CreatesSuccessfully()
    {
        // Act
        var annotation = Annotation.Create(1, 2, 3, "");

        // Assert
        annotation.Information.Should().Be("");
    }

    [Fact]
    public void CreatedAt_SetsDuringCreation()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var annotation = Annotation.Create(1, 2, 3, null);

        // Assert
        var afterCreate = DateTime.UtcNow;
        annotation.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        annotation.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var annotation = Annotation.Create(1, 2, 3, null);

        // Act
        annotation.ClearDomainEvents();

        // Assert
        annotation.DomainEvents.Should().BeEmpty();
    }

    #endregion
}
