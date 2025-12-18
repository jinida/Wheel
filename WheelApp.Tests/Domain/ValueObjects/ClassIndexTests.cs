using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects;

public class ClassIndexTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public void Create_ValidIndex_CreatesSuccessfully(int value)
    {
        // Act
        var classIndex = ClassIndex.Create(value);

        // Assert
        classIndex.Should().NotBeNull();
        classIndex.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-999)]
    [InlineData(int.MinValue)]
    public void Create_NegativeIndex_ThrowsValidationException(int value)
    {
        // Act
        var act = () => ClassIndex.Create(value);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var index1 = ClassIndex.Create(5);
        var index2 = ClassIndex.Create(5);

        // Act & Assert
        index1.Should().Be(index2);
        (index1 == index2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var index1 = ClassIndex.Create(5);
        var index2 = ClassIndex.Create(10);

        // Act & Assert
        index1.Should().NotBe(index2);
        (index1 == index2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHashCode()
    {
        // Arrange
        var index1 = ClassIndex.Create(5);
        var index2 = ClassIndex.Create(5);

        // Act & Assert
        index1.GetHashCode().Should().Be(index2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValue_ReturnsDifferentHashCode()
    {
        // Arrange
        var index1 = ClassIndex.Create(5);
        var index2 = ClassIndex.Create(10);

        // Act & Assert
        index1.GetHashCode().Should().NotBe(index2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        // Arrange
        var classIndex = ClassIndex.Create(42);

        // Act
        var result = classIndex.ToString();

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void ImplicitOperator_ConvertsToInt()
    {
        // Arrange
        var classIndex = ClassIndex.Create(42);

        // Act
        int result = classIndex;

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void Create_ZeroIndex_CreatesSuccessfully()
    {
        // Arrange & Act
        var classIndex = ClassIndex.Create(0);

        // Assert
        classIndex.Should().NotBeNull();
        classIndex.Value.Should().Be(0);
    }

    [Fact]
    public void Create_MaxValue_CreatesSuccessfully()
    {
        // Arrange & Act
        var classIndex = ClassIndex.Create(int.MaxValue);

        // Assert
        classIndex.Should().NotBeNull();
        classIndex.Value.Should().Be(int.MaxValue);
    }
}
