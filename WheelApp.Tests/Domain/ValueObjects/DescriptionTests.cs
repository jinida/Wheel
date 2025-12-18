using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects;

public class DescriptionTests
{
    [Theory]
    [InlineData("Short description")]
    [InlineData("This is a longer description with more details about the item")]
    [InlineData("")]
    public void Create_ValidDescription_CreatesSuccessfully(string description)
    {
        // Act
        var desc = Description.Create(description);

        // Assert
        desc.Should().NotBeNull();
        desc.Value.Should().Be(description);
    }

    [Fact]
    public void Create_NullDescription_CreatesWithNullValue()
    {
        // Act
        var desc = Description.Create(null);

        // Assert
        desc.Should().NotBeNull();
        desc.Value.Should().BeNull();
    }

    [Fact]
    public void Create_ExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange - max length is 255, create 256
        var longDesc = new string('A', 256);

        // Act
        var act = () => Description.Create(longDesc);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*255 characters*");
    }

    [Fact]
    public void Create_ExactlyMaxLength_CreatesSuccessfully()
    {
        // Arrange - max length is 255
        var maxDesc = new string('A', 255);

        // Act
        var desc = Description.Create(maxDesc);

        // Assert
        desc.Should().NotBeNull();
        desc.Value.Should().HaveLength(255);
        desc.Value.Should().Be(maxDesc);
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var desc1 = Description.Create("Test description");
        var desc2 = Description.Create("Test description");

        // Act & Assert
        desc1.Should().Be(desc2);
        (desc1 == desc2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var desc1 = Description.Create("Description 1");
        var desc2 = Description.Create("Description 2");

        // Act & Assert
        desc1.Should().NotBe(desc2);
        (desc1 == desc2).Should().BeFalse();
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        var desc1 = Description.Create(null);
        var desc2 = Description.Create(null);

        // Act & Assert
        desc1.Should().Be(desc2);
        (desc1 == desc2).Should().BeTrue();
    }

    [Fact]
    public void Equals_OneNull_ReturnsFalse()
    {
        // Arrange
        var desc1 = Description.Create(null);
        var desc2 = Description.Create("Some value");

        // Act & Assert
        desc1.Should().NotBe(desc2);
        (desc1 == desc2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHashCode()
    {
        // Arrange
        var desc1 = Description.Create("Test");
        var desc2 = Description.Create("Test");

        // Act & Assert
        desc1.GetHashCode().Should().Be(desc2.GetHashCode());
    }

    [Fact]
    public void ToString_WithValue_ReturnsValue()
    {
        // Arrange
        var desc = Description.Create("Test description");

        // Act
        var result = desc.ToString();

        // Assert
        result.Should().Be("Test description");
    }

    [Fact]
    public void ToString_WithNull_ReturnsEmptyString()
    {
        // Arrange
        var desc = Description.Create(null);

        // Act
        var result = desc.ToString();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ImplicitOperator_ConvertsToString()
    {
        // Arrange
        var desc = Description.Create("Test description");

        // Act
        string? result = desc;

        // Assert
        result.Should().Be("Test description");
    }

    [Fact]
    public void ImplicitOperator_WithNull_ReturnsNull()
    {
        // Arrange
        var desc = Description.Create(null);

        // Act
        string? result = desc;

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("   leading whitespace")]
    [InlineData("trailing whitespace   ")]
    [InlineData("  both  ")]
    public void Create_WithWhitespace_PreservesWhitespace(string description)
    {
        // Act
        var desc = Description.Create(description);

        // Assert
        desc.Value.Should().Be(description);
    }

    [Fact]
    public void Create_EmptyString_CreatesSuccessfully()
    {
        // Act
        var desc = Description.Create("");

        // Assert
        desc.Should().NotBeNull();
        desc.Value.Should().Be("");
    }
}
