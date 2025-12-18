using FluentAssertions;
using WheelApp.Application.UseCases.Annotations.Commands.ImportAnnotations;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Annotations.Commands;

/// <summary>
/// Tests for ImportAnnotationsCommandValidator
/// CRITICAL: Tests JSON validation for annotation import
/// </summary>
public class ImportAnnotationsCommandValidatorTests
{
    private readonly ImportAnnotationsCommandValidator _validator;

    public ImportAnnotationsCommandValidatorTests()
    {
        _validator = new ImportAnnotationsCommandValidator();
    }

    [Fact]
    public void Validate_ValidClassificationJson_Passes()
    {
        // Arrange
        var jsonContent = @"{
            ""header"": {
                ""version"": ""1.0.0"",
                ""type"": ""classification"",
                ""creator"": ""WheelApp"",
                ""categories"": [""cat"", ""dog""],
                ""description"": ""Test dataset""
            },
            ""annotations"": [
                {
                    ""filename"": ""image1.jpg"",
                    ""label"": 0,
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ValidObjectDetectionJson_Passes()
    {
        // Arrange
        var jsonContent = @"{
            ""header"": {
                ""type"": ""object_detection"",
                ""categories"": [""person"", ""car""]
            },
            ""annotations"": [
                {
                    ""filename"": ""image1.jpg"",
                    ""label"": [[0, 10, 20, 100, 200]],
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidSegmentationJson_Passes()
    {
        // Arrange
        var jsonContent = @"{
            ""header"": {
                ""type"": ""segmentation"",
                ""categories"": [""background"", ""object""]
            },
            ""annotations"": [
                {
                    ""filename"": ""image1.jpg"",
                    ""label"": [[0, 10, 20, 30, 40, 50, 60]],
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidAnomalyDetectionJson_Passes()
    {
        // Arrange
        var jsonContent = @"{
            ""header"": {
                ""type"": ""anomaly_detection"",
                ""categories"": [""normal"", ""defect""]
            },
            ""annotations"": [
                {
                    ""filename"": ""image1.jpg"",
                    ""label"": 1,
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidProjectId_Fails(int projectId)
    {
        // Arrange
        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat""]
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = projectId,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.ProjectId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyJsonContent_Fails(string? jsonContent)
    {
        // Arrange
        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent!
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.JsonContent));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("empty"));
    }

    [Fact]
    public void Validate_InvalidJson_Fails()
    {
        // Arrange - CRITICAL: Invalid JSON format
        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = "{ invalid json syntax }"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("valid JSON"));
    }

    [Fact]
    public void Validate_MissingHeaderProperty_Fails()
    {
        // Arrange - CRITICAL: JSON must have 'header' property
        var jsonContent = @"{
            ""annotations"": [
                {
                    ""filename"": ""image1.jpg"",
                    ""label"": 0,
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("header"));
    }

    [Fact]
    public void Validate_MissingAnnotationsProperty_Fails()
    {
        // Arrange - CRITICAL: JSON must have 'annotations' property
        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat""]
            }
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("annotations"));
    }

    [Fact]
    public void Validate_MissingTypeInHeader_Fails()
    {
        // Arrange - CRITICAL: Header must have 'type' property
        var jsonContent = @"{
            ""header"": {
                ""categories"": [""cat"", ""dog""]
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("type"));
    }

    [Theory]
    [InlineData("invalid_type")]
    [InlineData("yolo")]
    [InlineData("coco")]
    [InlineData("pascalvoc")]
    [InlineData("")]
    public void Validate_InvalidProjectType_Fails(string invalidType)
    {
        // Arrange - CRITICAL: Only specific project types are allowed
        var jsonContent = $@"{{
            ""header"": {{
                ""type"": ""{invalidType}"",
                ""categories"": [""cat""]
            }},
            ""annotations"": []
        }}";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("classification") ||
            e.ErrorMessage.Contains("object_detection") ||
            e.ErrorMessage.Contains("segmentation") ||
            e.ErrorMessage.Contains("anomaly_detection"));
    }

    [Fact]
    public void Validate_CaseInsensitiveProjectType_Passes()
    {
        // Arrange - Test case insensitivity
        var jsonContent = @"{
            ""header"": {
                ""type"": ""CLASSIFICATION"",
                ""categories"": [""cat""]
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCategoriesArray_Fails()
    {
        // Arrange - CRITICAL: Categories cannot be empty
        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": []
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("categories"));
    }

    [Fact]
    public void Validate_MissingCategoriesProperty_Fails()
    {
        // Arrange - CRITICAL: Header must have 'categories' property
        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification""
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("categories"));
    }

    [Fact]
    public void Validate_ComplexValidJson_Passes()
    {
        // Arrange - Test with multiple annotations
        var jsonContent = @"{
            ""header"": {
                ""version"": ""1.0.0"",
                ""type"": ""object_detection"",
                ""creator"": ""WheelApp"",
                ""categories"": [""person"", ""car"", ""bicycle""],
                ""description"": ""Complex test dataset""
            },
            ""annotations"": [
                {
                    ""filename"": ""image1.jpg"",
                    ""label"": [[0, 10, 20, 100, 200], [1, 50, 60, 150, 160]],
                    ""role"": 1
                },
                {
                    ""filename"": ""image2.jpg"",
                    ""label"": [[2, 30, 40, 130, 140]],
                    ""role"": 2
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MinimalValidJson_Passes()
    {
        // Arrange - Test with minimal required fields only
        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""class1""]
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_JsonWithSpecialCharactersInCategories_Passes()
    {
        // Arrange
        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""class-1"", ""class_2"", ""class 3""]
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = 1,
            JsonContent = jsonContent
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllProjectTypesWithUnderscores_Pass()
    {
        // Arrange & Act & Assert - Test all valid project types
        var projectTypes = new[] { "classification", "object_detection", "segmentation", "anomaly_detection" };

        foreach (var type in projectTypes)
        {
            var jsonContent = $@"{{
                ""header"": {{
                    ""type"": ""{type}"",
                    ""categories"": [""class1""]
                }},
                ""annotations"": []
            }}";

            var command = new ImportAnnotationsCommand
            {
                ProjectId = 1,
                JsonContent = jsonContent
            };

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue($"project type '{type}' should be valid");
        }
    }
}
