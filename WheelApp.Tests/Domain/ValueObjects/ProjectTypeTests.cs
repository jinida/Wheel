using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects;

public class ProjectTypeTests
{
    [Theory]
    [InlineData(0, "Classification")]
    [InlineData(1, "Object Detection")]
    [InlineData(2, "Segmentation")]
    [InlineData(3, "Anomaly Detection")]
    public void FromValue_ValidType_CreatesSuccessfully(int value, string expectedName)
    {
        // Act
        var projectType = ProjectType.FromValue(value);

        // Assert
        projectType.Should().NotBeNull();
        projectType.Value.Should().Be(value);
        projectType.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void FromValue_InvalidType_ThrowsInvalidProjectTypeException(int value)
    {
        // Act
        var act = () => ProjectType.FromValue(value);

        // Assert
        act.Should().Throw<InvalidProjectTypeException>()
            .Which.AttemptedValue.Should().Be(value);
    }

    [Fact]
    public void FromValue_InvalidType_ThrowsWithCorrectMessage()
    {
        // Arrange
        int invalidValue = 99;

        // Act
        var act = () => ProjectType.FromValue(invalidValue);

        // Assert
        act.Should().Throw<InvalidProjectTypeException>()
            .WithMessage("*99*")
            .WithMessage("*Classification*")
            .WithMessage("*ObjectDetection*")
            .WithMessage("*Segmentation*")
            .WithMessage("*AnomalyDetection*");
    }

    [Fact]
    public void Classification_ReturnsCorrectValues()
    {
        // Act
        var projectType = ProjectType.Classification;

        // Assert
        projectType.Value.Should().Be(0);
        projectType.Name.Should().Be("Classification");
    }

    [Fact]
    public void ObjectDetection_ReturnsCorrectValues()
    {
        // Act
        var projectType = ProjectType.ObjectDetection;

        // Assert
        projectType.Value.Should().Be(1);
        projectType.Name.Should().Be("Object Detection");
    }

    [Fact]
    public void Segmentation_ReturnsCorrectValues()
    {
        // Act
        var projectType = ProjectType.Segmentation;

        // Assert
        projectType.Value.Should().Be(2);
        projectType.Name.Should().Be("Segmentation");
    }

    [Fact]
    public void AnomalyDetection_ReturnsCorrectValues()
    {
        // Act
        var projectType = ProjectType.AnomalyDetection;

        // Assert
        projectType.Value.Should().Be(3);
        projectType.Name.Should().Be("Anomaly Detection");
    }

    [Fact]
    public void GetAll_ReturnsAllProjectTypes()
    {
        // Act
        var allTypes = ProjectType.GetAll().ToList();

        // Assert
        allTypes.Should().HaveCount(4);
        allTypes.Should().Contain(t => t.Value == 0 && t.Name == "Classification");
        allTypes.Should().Contain(t => t.Value == 1 && t.Name == "Object Detection");
        allTypes.Should().Contain(t => t.Value == 2 && t.Name == "Segmentation");
        allTypes.Should().Contain(t => t.Value == 3 && t.Name == "Anomaly Detection");
    }

    [Fact]
    public void Equals_SameType_ReturnsTrue()
    {
        // Arrange
        var type1 = ProjectType.FromValue(0);
        var type2 = ProjectType.FromValue(0);

        // Act & Assert
        type1.Should().Be(type2);
        (type1 == type2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var type1 = ProjectType.FromValue(0);
        var type2 = ProjectType.FromValue(1);

        // Act & Assert
        type1.Should().NotBe(type2);
        (type1 == type2).Should().BeFalse();
    }

    [Fact]
    public void Equals_StaticPropertyAndFromValue_ReturnsTrue()
    {
        // Arrange
        var type1 = ProjectType.Classification;
        var type2 = ProjectType.FromValue(0);

        // Act & Assert
        type1.Should().Be(type2);
    }

    [Fact]
    public void GetHashCode_SameType_ReturnsSameHashCode()
    {
        // Arrange
        var type1 = ProjectType.FromValue(2);
        var type2 = ProjectType.FromValue(2);

        // Act & Assert
        type1.GetHashCode().Should().Be(type2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        // Arrange
        var projectType = ProjectType.Segmentation;

        // Act
        var result = projectType.ToString();

        // Assert
        result.Should().Be("Segmentation");
    }

    [Fact]
    public void ImplicitOperator_ConvertsToInt()
    {
        // Arrange
        var projectType = ProjectType.ObjectDetection;

        // Act
        int result = projectType;

        // Assert
        result.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FromValue_MultipleCallsSameValue_ReturnsSameInstance(int value)
    {
        // Act
        var type1 = ProjectType.FromValue(value);
        var type2 = ProjectType.FromValue(value);

        // Assert
        type1.Should().Be(type2);
        ReferenceEquals(type1, type2).Should().BeTrue();
    }

    [Fact]
    public void InvalidProjectTypeException_InheritsFromValidationException()
    {
        // Arrange
        var exception = new InvalidProjectTypeException(99);

        // Assert
        exception.Should().BeOfType<InvalidProjectTypeException>();
        exception.Should().BeAssignableTo<ValidationException>();
    }
}
