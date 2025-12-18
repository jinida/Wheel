using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects
{
    /// <summary>
    /// CRITICAL SECURITY TESTS: File path validation and path traversal prevention
    /// </summary>
    public class FilePathTests
    {
        [Theory]
        [InlineData("images/test.jpg")]
        [InlineData("uploads/dataset1/image001.png")]
        [InlineData("data/photos/pic.jpeg")]
        [InlineData("images/subfolder/file.jpg")]
        [InlineData("test.jpg")]
        public void Create_ValidPath_CreatesSuccessfully(string path)
        {
            // Act
            var filePath = FilePath.Create(path);

            // Assert
            filePath.Should().NotBeNull();
            filePath.Value.Should().Be(path);
        }

        [Theory]
        [InlineData("../")]
        [InlineData("..\\")]
        [InlineData("../../etc/passwd")]
        [InlineData("..\\..\\windows\\system32")]
        [InlineData("images/../../../etc/passwd")]
        [InlineData("uploads\\..\\..\\sensitive")]
        [InlineData("folder/../../file.txt")]
        [InlineData("../parent/file.jpg")]
        public void Create_PathTraversalAttempt_ThrowsValidationException(string maliciousPath)
        {
            // CRITICAL SECURITY TEST - Prevent path traversal attacks
            // Act
            var act = () => FilePath.Create(maliciousPath);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*invalid sequences*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_EmptyOrWhitespace_ThrowsValidationException(string path)
        {
            // Act
            var act = () => FilePath.Create(path);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*cannot be empty*");
        }

        [Fact]
        public void Create_ExceedsMaxLength_ThrowsValidationException()
        {
            // Arrange - Max is 512 characters
            var longPath = new string('a', 513);

            // Act
            var act = () => FilePath.Create(longPath);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*512 characters*");
        }

        [Theory]
        [InlineData("image<test>.jpg")]      // < and >
        [InlineData("image:test.jpg")]       // Colon
        [InlineData("image\"test\".jpg")]    // Double quote
        [InlineData("image|test.jpg")]       // Pipe
        [InlineData("image?test.jpg")]       // Question mark
        [InlineData("image*test.jpg")]       // Asterisk
        [InlineData("image\0.jpg")]          // Null byte
        [InlineData("image\n.jpg")]          // Newline
        [InlineData("image\r.jpg")]          // Carriage return
        [InlineData("image\t.jpg")]          // Tab
        public void Create_DangerousCharacters_ThrowsValidationException(string pathWithDangerousChars)
        {
            // CRITICAL SECURITY TEST - Prevent dangerous characters
            // Act
            var act = () => FilePath.Create(pathWithDangerousChars);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*invalid characters*");
        }

        [Theory]
        [InlineData("path//with//double//slashes.jpg")]
        [InlineData("path\\\\with\\\\double\\\\backslashes.jpg")]
        public void Create_DoubleSlashes_ThrowsValidationException(string pathWithDoubleSlashes)
        {
            // CRITICAL SECURITY TEST - Prevent double slashes
            // Act
            var act = () => FilePath.Create(pathWithDoubleSlashes);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*invalid sequences*");
        }

        [Fact]
        public void Equals_SameValue_ReturnsTrue()
        {
            // Arrange
            var path1 = FilePath.Create("images/test.jpg");
            var path2 = FilePath.Create("images/test.jpg");

            // Act & Assert
            path1.Should().Be(path2);
            (path1 == path2).Should().BeTrue();
            path1.GetHashCode().Should().Be(path2.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_ReturnsFalse()
        {
            // Arrange
            var path1 = FilePath.Create("images/test1.jpg");
            var path2 = FilePath.Create("images/test2.jpg");

            // Act & Assert
            path1.Should().NotBe(path2);
            (path1 == path2).Should().BeFalse();
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            // Arrange
            var filePath = FilePath.Create("images/test.jpg");

            // Act
            var result = filePath.ToString();

            // Assert
            result.Should().Be("images/test.jpg");
        }

        [Fact]
        public void ImplicitConversion_ToString_WorksCorrectly()
        {
            // Arrange
            var filePath = FilePath.Create("images/test.jpg");

            // Act
            string result = filePath;

            // Assert
            result.Should().Be("images/test.jpg");
        }

        [Fact]
        public void Create_ValidPathWithSpaces_CreatesSuccessfully()
        {
            // Arrange
            var path = "images/my test file.jpg";

            // Act
            var filePath = FilePath.Create(path);

            // Assert
            filePath.Value.Should().Be(path);
        }

        [Fact]
        public void Create_ValidPathWithDashes_CreatesSuccessfully()
        {
            // Arrange
            var path = "images/test-file-name.jpg";

            // Act
            var filePath = FilePath.Create(path);

            // Assert
            filePath.Value.Should().Be(path);
        }

        [Fact]
        public void Create_ValidPathWithUnderscores_CreatesSuccessfully()
        {
            // Arrange
            var path = "images/test_file_name.jpg";

            // Act
            var filePath = FilePath.Create(path);

            // Assert
            filePath.Value.Should().Be(path);
        }
    }
}
