using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for ProjectClass entity - MEDIUM PRIORITY: Classification class management
/// </summary>
public class ProjectClassTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_ValidProjectClass_CreatesSuccessfully()
    {
        // Arrange
        var projectId = 1;
        var classIdx = 0;
        var name = "Person";
        var color = "#FF0000";

        // Act
        var projectClass = ProjectClass.Create(projectId, classIdx, name, color);

        // Assert
        projectClass.Should().NotBeNull();
        projectClass.ProjectId.Should().Be(projectId);
        projectClass.ClassIdx.Value.Should().Be(classIdx);
        projectClass.Name.Should().Be(name);
        projectClass.Color.Value.Should().Be(color.ToUpper());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_InvalidProjectId_ThrowsValidationException(int invalidProjectId)
    {
        // Act
        var act = () => ProjectClass.Create(invalidProjectId, 0, "Person", "#FF0000");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Project ID*cannot be negative*");
    }

    [Fact]
    public void Create_ProjectIdZero_AllowedForDomainTests()
    {
        // Act
        var projectClass = ProjectClass.Create(0, 0, "Person", "#FF0000");

        // Assert
        projectClass.ProjectId.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceName_ThrowsValidationException(string invalidName)
    {
        // Act
        var act = () => ProjectClass.Create(1, 0, invalidName, "#FF0000");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class name*cannot be empty*");
    }

    [Fact]
    public void Create_NameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var longName = new string('A', 31);  // Max is 30

        // Act
        var act = () => ProjectClass.Create(1, 0, longName, "#FF0000");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class name*cannot exceed 30 characters*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_NegativeClassIndex_ThrowsValidationException(int negativeIndex)
    {
        // Act
        var act = () => ProjectClass.Create(1, negativeIndex, "Person", "#FF0000");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class index*cannot be negative*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceColor_ThrowsValidationException(string invalidColor)
    {
        // Act
        var act = () => ProjectClass.Create(1, 0, "Person", invalidColor);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Color code*cannot be empty*");
    }

    [Theory]
    [InlineData("FF0000")]         // Missing #
    [InlineData("#FF00")]           // Too short
    [InlineData("#FF00000")]        // Too long
    [InlineData("#GGGGGG")]         // Invalid hex
    [InlineData("red")]             // Not hex format
    [InlineData("#FF-000")]         // Invalid character
    public void Create_InvalidColorFormat_ThrowsValidationException(string invalidColor)
    {
        // Act
        var act = () => ProjectClass.Create(1, 0, "Person", invalidColor);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Color code*must be in #RRGGBB format*");
    }

    [Theory]
    [InlineData("#ff0000", "#FF0000")]
    [InlineData("#aaBBcc", "#AABBCC")]
    [InlineData("#123abc", "#123ABC")]
    public void Create_LowercaseColor_ConvertsToUppercase(string inputColor, string expectedColor)
    {
        // Act
        var projectClass = ProjectClass.Create(1, 0, "Person", inputColor);

        // Assert
        projectClass.Color.Value.Should().Be(expectedColor);
    }

    #endregion

    #region UpdateColor Tests

    [Fact]
    public void UpdateColor_ValidColor_UpdatesSuccessfully()
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");
        var newColor = "#00FF00";

        // Act
        projectClass.UpdateColor(newColor);

        // Assert
        projectClass.Color.Value.Should().Be(newColor);
    }

    [Theory]
    [InlineData("#ff0000", "#FF0000")]
    [InlineData("#aabbcc", "#AABBCC")]
    public void UpdateColor_LowercaseColor_ConvertsToUppercase(string inputColor, string expectedColor)
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Act
        projectClass.UpdateColor(inputColor);

        // Assert
        projectClass.Color.Value.Should().Be(expectedColor);
    }

    [Theory]
    [InlineData("FF0000")]
    [InlineData("#FF00")]
    [InlineData("red")]
    public void UpdateColor_InvalidColor_ThrowsValidationException(string invalidColor)
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Act
        var act = () => projectClass.UpdateColor(invalidColor);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Color code*must be in #RRGGBB format*");
    }

    #endregion

    #region UpdateName Tests

    [Fact]
    public void UpdateName_ValidName_UpdatesSuccessfully()
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");
        var newName = "Car";

        // Act
        projectClass.UpdateName(newName);

        // Assert
        projectClass.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_EmptyOrWhitespaceName_ThrowsValidationException(string invalidName)
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Act
        var act = () => projectClass.UpdateName(invalidName);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class name*cannot be empty*");
    }

    [Fact]
    public void UpdateName_NameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");
        var longName = new string('A', 31);  // Max is 30

        // Act
        var act = () => projectClass.UpdateName(longName);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class name*cannot exceed 30 characters*");
    }

    #endregion

    #region UpdateClassIdx Tests

    [Fact]
    public void UpdateClassIdx_ValidIndex_UpdatesSuccessfully()
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");
        var newIndex = 5;

        // Act
        projectClass.UpdateClassIdx(newIndex);

        // Assert
        projectClass.ClassIdx.Value.Should().Be(newIndex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void UpdateClassIdx_ValidPositiveIndices_UpdatesSuccessfully(int validIndex)
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Act
        projectClass.UpdateClassIdx(validIndex);

        // Assert
        projectClass.ClassIdx.Value.Should().Be(validIndex);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UpdateClassIdx_NegativeIndex_ThrowsValidationException(int negativeIndex)
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Act
        var act = () => projectClass.UpdateClassIdx(negativeIndex);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Class index*cannot be negative*");
    }

    #endregion

    #region Navigation Properties Tests

    [Fact]
    public void Project_InitiallyNull()
    {
        // Arrange & Act
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Assert
        projectClass.Project.Should().BeNull();
    }

    [Fact]
    public void Annotations_InitiallyEmpty()
    {
        // Arrange & Act
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Assert
        projectClass.Annotations.Should().BeEmpty();
    }

    [Fact]
    public void Annotations_ReturnsReadOnlyCollection()
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Act & Assert
        projectClass.Annotations.Should().BeAssignableTo<IReadOnlyCollection<Annotation>>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Name_ExactlyMaxLength_CreatesSuccessfully()
    {
        // Arrange
        var maxLengthName = new string('A', 30);

        // Act
        var projectClass = ProjectClass.Create(1, 0, maxLengthName, "#FF0000");

        // Assert
        projectClass.Name.Should().Be(maxLengthName);
    }

    [Fact]
    public void ClassIdx_ReturnsClassIndexValueObject()
    {
        // Act
        var projectClass = ProjectClass.Create(1, 5, "Person", "#FF0000");

        // Assert
        projectClass.ClassIdx.Should().BeOfType<ClassIndex>();
        projectClass.ClassIdx.Value.Should().Be(5);
    }

    [Fact]
    public void Color_ReturnsColorCodeValueObject()
    {
        // Act
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Assert
        projectClass.Color.Should().BeOfType<ColorCode>();
        projectClass.Color.Value.Should().Be("#FF0000");
    }

    [Theory]
    [InlineData("Person")]
    [InlineData("Car")]
    [InlineData("Background")]
    [InlineData("Class 1")]
    [InlineData("Object-123")]
    public void Create_VariousValidNames_CreatesSuccessfully(string validName)
    {
        // Act
        var projectClass = ProjectClass.Create(1, 0, validName, "#FF0000");

        // Assert
        projectClass.Name.Should().Be(validName);
    }

    [Theory]
    [InlineData("#FF0000")]  // Red
    [InlineData("#00FF00")]  // Green
    [InlineData("#0000FF")]  // Blue
    [InlineData("#FFFFFF")]  // White
    [InlineData("#000000")]  // Black
    [InlineData("#FFFF00")]  // Yellow
    [InlineData("#FF00FF")]  // Magenta
    [InlineData("#00FFFF")]  // Cyan
    public void Create_VariousValidColors_CreatesSuccessfully(string validColor)
    {
        // Act
        var projectClass = ProjectClass.Create(1, 0, "Person", validColor);

        // Assert
        projectClass.Color.Value.Should().Be(validColor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(99)]
    public void Create_VariousValidIndices_CreatesSuccessfully(int validIndex)
    {
        // Act
        var projectClass = ProjectClass.Create(1, validIndex, "Person", "#FF0000");

        // Assert
        projectClass.ClassIdx.Value.Should().Be(validIndex);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var projectClass = ProjectClass.Create(1, 0, "Person", "#FF0000");

        // Act
        projectClass.ClearDomainEvents();

        // Assert
        projectClass.DomainEvents.Should().BeEmpty();
    }

    #endregion
}
