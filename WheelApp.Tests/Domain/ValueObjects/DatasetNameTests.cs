using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects
{
    /// <summary>
    /// Tests for DatasetName value object validation
    /// </summary>
    public class DatasetNameTests
    {
        [Theory]
        [InlineData("Dataset1")]
        [InlineData("My Dataset")]
        [InlineData("Test_Dataset")]
        [InlineData("Dataset-123")]
        [InlineData("Test Dataset 2024")]
        [InlineData("Dataset")]
        [InlineData("A")]
        [InlineData("Dataset   With   Spaces")]
        public void Create_ValidName_CreatesSuccessfully(string name)
        {
            // Act
            var datasetName = DatasetName.Create(name);

            // Assert
            datasetName.Should().NotBeNull();
            datasetName.Value.Should().Be(name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_EmptyOrWhitespace_ThrowsValidationException(string name)
        {
            // Act
            var act = () => DatasetName.Create(name);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*cannot be empty*");
        }

        [Fact]
        public void Create_ExceedsMaxLength_ThrowsValidationException()
        {
            // Arrange - Max is 50 characters
            var longName = new string('A', 51);

            // Act
            var act = () => DatasetName.Create(longName);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*50 characters*");
        }

        [Fact]
        public void Create_MaxLength_CreatesSuccessfully()
        {
            // Arrange - Exactly 50 characters
            var name = new string('A', 50);

            // Act
            var datasetName = DatasetName.Create(name);

            // Assert
            datasetName.Value.Should().Be(name);
            datasetName.Value.Length.Should().Be(50);
        }

        [Theory]
        [InlineData("Dataset<Test>")]     // < and >
        [InlineData("Dataset:Test")]      // Colon
        [InlineData("Dataset\"Test\"")]   // Double quote
        [InlineData("Dataset/Test")]      // Forward slash
        [InlineData("Dataset\\Test")]     // Backslash
        [InlineData("Dataset|Test")]      // Pipe
        [InlineData("Dataset?Test")]      // Question mark
        [InlineData("Dataset*Test")]      // Asterisk
        public void Create_InvalidFileSystemCharacters_ThrowsValidationException(string name)
        {
            // These characters are invalid for file system names
            // Act
            var act = () => DatasetName.Create(name);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*invalid characters*");
        }

        [Fact]
        public void Equals_SameValue_ReturnsTrue()
        {
            // Arrange
            var name1 = DatasetName.Create("Test Dataset");
            var name2 = DatasetName.Create("Test Dataset");

            // Act & Assert
            name1.Should().Be(name2);
            (name1 == name2).Should().BeTrue();
            name1.GetHashCode().Should().Be(name2.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_ReturnsFalse()
        {
            // Arrange
            var name1 = DatasetName.Create("Dataset1");
            var name2 = DatasetName.Create("Dataset2");

            // Act & Assert
            name1.Should().NotBe(name2);
            (name1 == name2).Should().BeFalse();
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            // Arrange
            var datasetName = DatasetName.Create("Test Dataset");

            // Act
            var result = datasetName.ToString();

            // Assert
            result.Should().Be("Test Dataset");
        }

        [Fact]
        public void ImplicitConversion_ToString_WorksCorrectly()
        {
            // Arrange
            var datasetName = DatasetName.Create("Test Dataset");

            // Act
            string result = datasetName;

            // Assert
            result.Should().Be("Test Dataset");
        }

        [Theory]
        [InlineData("Dataset With Spaces")]
        [InlineData("Dataset_With_Underscores")]
        [InlineData("Dataset-With-Dashes")]
        [InlineData("Dataset123")]
        [InlineData("123Dataset")]
        public void Create_ValidMixedCharacters_CreatesSuccessfully(string name)
        {
            // Act
            var datasetName = DatasetName.Create(name);

            // Assert
            datasetName.Value.Should().Be(name);
        }

        [Fact]
        public void Create_SingleCharacter_CreatesSuccessfully()
        {
            // Act
            var datasetName = DatasetName.Create("A");

            // Assert
            datasetName.Value.Should().Be("A");
        }

        [Fact]
        public void Create_NumericOnly_CreatesSuccessfully()
        {
            // Arrange
            var name = "12345";

            // Act
            var datasetName = DatasetName.Create(name);

            // Assert
            datasetName.Value.Should().Be(name);
        }

        [Fact]
        public void Create_TrailingAndLeadingSpaces_CreatesSuccessfully()
        {
            // Arrange - The implementation doesn't trim, so spaces are preserved
            var name = " Dataset ";

            // Act
            var datasetName = DatasetName.Create(name);

            // Assert
            datasetName.Value.Should().Be(name);
        }

        [Theory]
        [InlineData("Dataset\0Test")]     // Null byte - NOT rejected by current regex
        [InlineData("Dataset\nTest")]     // Newline - NOT rejected by current regex
        [InlineData("Dataset\rTest")]     // Carriage return - NOT rejected by current regex
        [InlineData("Dataset\tTest")]     // Tab - allowed (whitespace character)
        public void Create_ControlCharacters_CreatesSuccessfully(string name)
        {
            // NOTE: The current regex pattern @"[<>:""/\\|?*]" does NOT include control characters
            // These characters are currently ALLOWED by the implementation
            // If control character validation is needed, the Domain code needs to be updated

            // Act
            var datasetName = DatasetName.Create(name);

            // Assert
            datasetName.Should().NotBeNull();
            datasetName.Value.Should().Be(name);
        }
    }
}
