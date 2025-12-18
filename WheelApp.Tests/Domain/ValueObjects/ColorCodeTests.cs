using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects;

public class ColorCodeTests
{
    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#00FF00")]
    [InlineData("#0000FF")]
    [InlineData("#FFFFFF")]
    [InlineData("#000000")]
    [InlineData("#ABCDEF")]
    [InlineData("#123456")]
    public void Create_ValidHexColor_CreatesSuccessfully(string color)
    {
        // Act
        var colorCode = ColorCode.Create(color);

        // Assert
        colorCode.Should().NotBeNull();
        colorCode.Value.Should().Be(color.ToUpper());
    }

    [Theory]
    [InlineData("#ff0000")]  // Lowercase
    [InlineData("#Ff0000")]  // Mixed case
    [InlineData("#aBcDeF")]  // Mixed case
    public void Create_LowercaseHex_ConvertsToUppercase(string color)
    {
        // Act
        var colorCode = ColorCode.Create(color);

        // Assert
        colorCode.Value.Should().Be(color.ToUpper());
    }

    [Theory]
    [InlineData("FF0000")]      // Missing #
    [InlineData("#FFF")]        // Too short
    [InlineData("#FFFFFFF")]    // Too long
    [InlineData("#GGGGGG")]     // Invalid hex
    [InlineData("#XYZ123")]     // Invalid hex characters
    [InlineData("red")]         // Color name
    [InlineData("#12-456")]     // Invalid characters
    public void Create_InvalidFormat_ThrowsValidationException(string color)
    {
        // Act
        var act = () => ColorCode.Create(color);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*#RRGGBB*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespace_ThrowsValidationException(string color)
    {
        // Act
        var act = () => ColorCode.Create(color);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Equals_SameColor_ReturnsTrue()
    {
        // Arrange
        var color1 = ColorCode.Create("#FF0000");
        var color2 = ColorCode.Create("#FF0000");

        // Act & Assert
        color1.Should().Be(color2);
        (color1 == color2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentColor_ReturnsFalse()
    {
        // Arrange
        var color1 = ColorCode.Create("#FF0000");
        var color2 = ColorCode.Create("#00FF00");

        // Act & Assert
        color1.Should().NotBe(color2);
        (color1 == color2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameColor_ReturnsSameHashCode()
    {
        // Arrange
        var color1 = ColorCode.Create("#FF0000");
        var color2 = ColorCode.Create("#FF0000");

        // Act & Assert
        color1.GetHashCode().Should().Be(color2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var color = ColorCode.Create("#FF0000");

        // Act
        var result = color.ToString();

        // Assert
        result.Should().Be("#FF0000");
    }

    [Fact]
    public void ImplicitOperator_ConvertsToString()
    {
        // Arrange
        var color = ColorCode.Create("#FF0000");

        // Act
        string result = color;

        // Assert
        result.Should().Be("#FF0000");
    }

    [Fact]
    public void Equals_CaseInsensitive_ReturnsTrue()
    {
        // Arrange - lowercase input converted to uppercase
        var color1 = ColorCode.Create("#ff0000");
        var color2 = ColorCode.Create("#FF0000");

        // Act & Assert
        color1.Should().Be(color2);
        color1.Value.Should().Be("#FF0000");
        color2.Value.Should().Be("#FF0000");
    }
}
