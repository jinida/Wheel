using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects
{
    /// <summary>
    /// Tests for ProjectName value object validation
    /// </summary>
    public class ProjectNameTests
    {
        [Theory]
        [InlineData("Project1")]
        [InlineData("My Project")]
        [InlineData("Test_Project")]
        [InlineData("Project-123")]
        [InlineData("Test Project 2024")]
        [InlineData("Project")]
        [InlineData("A")]
        [InlineData("Project   With   Spaces")]
        public void Create_ValidName_CreatesSuccessfully(string name)
        {
            // Act
            var projectName = ProjectName.Create(name);

            // Assert
            projectName.Should().NotBeNull();
            projectName.Value.Should().Be(name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_EmptyOrWhitespace_ThrowsValidationException(string name)
        {
            // Act
            var act = () => ProjectName.Create(name);

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
            var act = () => ProjectName.Create(longName);

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
            var projectName = ProjectName.Create(name);

            // Assert
            projectName.Value.Should().Be(name);
            projectName.Value.Length.Should().Be(50);
        }

        [Theory]
        [InlineData("Project<Test>")]     // < and >
        [InlineData("Project:Test")]      // Colon
        [InlineData("Project\"Test\"")]   // Double quote
        [InlineData("Project/Test")]      // Forward slash
        [InlineData("Project\\Test")]     // Backslash
        [InlineData("Project|Test")]      // Pipe
        [InlineData("Project?Test")]      // Question mark
        [InlineData("Project*Test")]      // Asterisk
        public void Create_InvalidFileSystemCharacters_ThrowsValidationException(string name)
        {
            // These characters are invalid for file system names
            // Act
            var act = () => ProjectName.Create(name);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*invalid characters*");
        }

        [Fact]
        public void Equals_SameValue_ReturnsTrue()
        {
            // Arrange
            var name1 = ProjectName.Create("Test Project");
            var name2 = ProjectName.Create("Test Project");

            // Act & Assert
            name1.Should().Be(name2);
            (name1 == name2).Should().BeTrue();
            name1.GetHashCode().Should().Be(name2.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_ReturnsFalse()
        {
            // Arrange
            var name1 = ProjectName.Create("Project1");
            var name2 = ProjectName.Create("Project2");

            // Act & Assert
            name1.Should().NotBe(name2);
            (name1 == name2).Should().BeFalse();
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            // Arrange
            var projectName = ProjectName.Create("Test Project");

            // Act
            var result = projectName.ToString();

            // Assert
            result.Should().Be("Test Project");
        }

        [Fact]
        public void ImplicitConversion_ToString_WorksCorrectly()
        {
            // Arrange
            var projectName = ProjectName.Create("Test Project");

            // Act
            string result = projectName;

            // Assert
            result.Should().Be("Test Project");
        }

        [Theory]
        [InlineData("Project With Spaces")]
        [InlineData("Project_With_Underscores")]
        [InlineData("Project-With-Dashes")]
        [InlineData("Project123")]
        [InlineData("123Project")]
        public void Create_ValidMixedCharacters_CreatesSuccessfully(string name)
        {
            // Act
            var projectName = ProjectName.Create(name);

            // Assert
            projectName.Value.Should().Be(name);
        }

        [Fact]
        public void Create_SingleCharacter_CreatesSuccessfully()
        {
            // Act
            var projectName = ProjectName.Create("A");

            // Assert
            projectName.Value.Should().Be("A");
        }

        [Fact]
        public void Create_NumericOnly_CreatesSuccessfully()
        {
            // Arrange
            var name = "12345";

            // Act
            var projectName = ProjectName.Create(name);

            // Assert
            projectName.Value.Should().Be(name);
        }

        [Fact]
        public void Create_TrailingAndLeadingSpaces_CreatesSuccessfully()
        {
            // Arrange - The implementation doesn't trim, so spaces are preserved
            var name = " Project ";

            // Act
            var projectName = ProjectName.Create(name);

            // Assert
            projectName.Value.Should().Be(name);
        }

        [Theory]
        [InlineData("Project\0Test")]     // Null byte - NOT rejected by current regex
        [InlineData("Project\nTest")]     // Newline - NOT rejected by current regex
        [InlineData("Project\rTest")]     // Carriage return - NOT rejected by current regex
        [InlineData("Project\tTest")]     // Tab - allowed (whitespace character)
        public void Create_ControlCharacters_CreatesSuccessfully(string name)
        {
            // NOTE: The current regex pattern @"[<>:""/\\|?*]" does NOT include control characters
            // These characters are currently ALLOWED by the implementation
            // If control character validation is needed, the Domain code needs to be updated

            // Act
            var projectName = ProjectName.Create(name);

            // Assert
            projectName.Should().NotBeNull();
            projectName.Value.Should().Be(name);
        }

        [Fact]
        public void Equals_ProjectNameAndDatasetNameWithSameValue_AreNotEqual()
        {
            // This test verifies that ProjectName and DatasetName are different types
            // even if they have the same validation rules
            // Arrange
            var projectName = ProjectName.Create("Test");
            var datasetName = DatasetName.Create("Test");

            // Act & Assert
            // They should not be equal as they are different value object types
            projectName.Should().NotBe((object)datasetName);
        }
    }
}
