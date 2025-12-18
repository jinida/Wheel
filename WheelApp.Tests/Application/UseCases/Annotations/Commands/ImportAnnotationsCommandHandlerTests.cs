using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.UseCases.Annotations.Commands.ImportAnnotations;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Annotations.Commands;

/// <summary>
/// Tests for ImportAnnotationsCommandHandler
/// CRITICAL: Tests complex JSON import logic with multiple formats (classification, detection, segmentation, anomaly)
/// </summary>
public class ImportAnnotationsCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly ImportAnnotationsCommandHandler _handler;
    private readonly ILogger<ImportAnnotationsCommandHandler> _logger;

    public ImportAnnotationsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WheelAppDbContext(options);
        _logger = Substitute.For<ILogger<ImportAnnotationsCommandHandler>>();

        // Create repositories with real implementations
        var projectRepository = new WheelApp.Infrastructure.Persistence.Repositories.ProjectRepository(_context);
        var classRepository = new WheelApp.Infrastructure.Persistence.Repositories.ProjectClassRepository(_context);
        var imageRepository = new WheelApp.Infrastructure.Persistence.Repositories.ImageRepository(_context);
        var annotationRepository = new WheelApp.Infrastructure.Persistence.Repositories.AnnotationRepository(_context);
        var roleRepository = new WheelApp.Infrastructure.Persistence.Repositories.RoleRepository(_context);
        var unitOfWork = new WheelApp.Infrastructure.Persistence.UnitOfWork(_context);

        _handler = new ImportAnnotationsCommandHandler(
            projectRepository,
            classRepository,
            imageRepository,
            annotationRepository,
            roleRepository,
            unitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_ClassificationFormat_ImportsSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Add classes
        var class1 = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "dog", "#00FF00");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        // Add images
        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat"", ""dog""]
            },
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(1);
        result.Value.FailedCount.Should().Be(0);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().HaveCount(1);
        annotations[0].ImageId.Should().Be(image1.Id);
        annotations[0].ProjectId.Should().Be(project.Id);
        annotations[0].Information.Should().BeNull(); // Classification has no coordinates
    }

    [Fact]
    public async Task Handle_ObjectDetectionFormat_ImportsSuccessfully()
    {
        // Arrange - label: [[classIdx, x1, y1, x2, y2], ...]
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, "Test", dataset.Id, "user1"); // Type 1 = Object Detection
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "person", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "car", "#00FF00");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""object_detection"",
                ""categories"": [""person"", ""car""]
            },
            ""annotations"": [
                {
                    ""filename"": ""img1.jpg"",
                    ""label"": [[0, 10, 20, 100, 200], [1, 50, 60, 150, 160]],
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(1);
        result.Value.FailedCount.Should().Be(0);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().HaveCount(2); // Two bounding boxes
        annotations.Should().OnlyContain(a => a.Information != null);

        // Verify coordinate conversion: [cls, x1, y1, x2, y2] -> [[x1,y1],[x2,y2]]
        annotations[0].Information.Should().Contain("10");
        annotations[0].Information.Should().Contain("20");
        annotations[0].Information.Should().Contain("100");
        annotations[0].Information.Should().Contain("200");
    }

    [Fact]
    public async Task Handle_SegmentationFormat_ImportsSuccessfully()
    {
        // Arrange - label: [[classIdx, x1, y1, x2, y2, ...], ...]
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 2, "Test", dataset.Id, "user1"); // Type 2 = Segmentation
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "background", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "object", "#00FF00");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""segmentation"",
                ""categories"": [""background"", ""object""]
            },
            ""annotations"": [
                {
                    ""filename"": ""img1.jpg"",
                    ""label"": [[0, 10, 20, 30, 40, 50, 60]],
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(1);
        result.Value.FailedCount.Should().Be(0);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().HaveCount(1);
        annotations[0].Information.Should().NotBeNull();
        annotations[0].Information.Should().Contain("10");
        annotations[0].Information.Should().Contain("20");
        annotations[0].Information.Should().Contain("30");
    }

    [Fact]
    public async Task Handle_AnomalyDetectionFormat_ImportsSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 3, "Test", dataset.Id, "user1"); // Type 3 = Anomaly Detection
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "normal", "#00FF00");
        var class2 = ProjectClass.Create(project.Id, 1, "defect", "#FF0000");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/1/img2.jpg", dataset.Id);
        _context.Images.AddRange(image1, image2);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""anomaly_detection"",
                ""categories"": [""normal"", ""defect""]
            },
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 },
                { ""filename"": ""img2.jpg"", ""label"": 1, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(0);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().HaveCount(2);
        annotations.Should().OnlyContain(a => a.Information == null); // Anomaly detection has no coordinates
    }

    [Fact]
    public async Task Handle_MissingClass_CreatesAutomatically()
    {
        // Arrange - CRITICAL: Auto-create missing ProjectClasses
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""newclass""]
            },
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(1);
        result.Value.Messages.Should().Contain(m => m.Contains("Created new class: 'newclass'"));

        var classes = await _context.ProjectClasses.ToListAsync();
        classes.Should().HaveCount(1);
        classes[0].Name.Should().Be("newclass");
        classes[0].ClassIdx.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = "invalid json { this is not valid"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("parse JSON");
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new ImportAnnotationsCommand
        {
            ProjectId = 999,
            JsonContent = @"{""header"":{""type"":""classification"",""categories"":[""cat""]},""annotations"":[]}"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project");
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TypeMismatch_ReturnsFailure()
    {
        // Arrange - Project is classification but JSON is object_detection
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1"); // Type 0 = Classification
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""object_detection"",
                ""categories"": [""person""]
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("type mismatch");
        result.Error.Should().Contain("classification");
        result.Error.Should().Contain("object_detection");
    }

    [Fact]
    public async Task Handle_ImageNotFound_SkipsAnnotation()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(class1);
        await _context.SaveChangesAsync();

        // No images added

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat""]
            },
            ""annotations"": [
                { ""filename"": ""nonexistent.jpg"", ""label"": 0, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(1);
        result.Value.FailedItems.Should().Contain("nonexistent.jpg");
        result.Value.Messages.Should().Contain(m => m.Contains("not found"));
    }

    [Fact]
    public async Task Handle_DetectionCoordinateConversion_ConvertsCorrectly()
    {
        // Arrange - CRITICAL: [cls, x1, y1, x2, y2] -> [[x1,y1],[x2,y2]]
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, "Test", dataset.Id, "user1"); // Object Detection
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "obj", "#FF0000");
        _context.ProjectClasses.Add(class1);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""object_detection"",
                ""categories"": [""obj""]
            },
            ""annotations"": [
                {
                    ""filename"": ""img1.jpg"",
                    ""label"": [[0, 10, 20, 100, 200]],
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var annotation = await _context.Annotations.FirstAsync();
        annotation.Information.Should().NotBeNull();

        // Verify format: [[10,20],[100,200]]
        annotation.Information.Should().Contain("[10,20]");
        annotation.Information.Should().Contain("[100,200]");
        annotation.Information.Should().NotContain("[0,"); // Class index removed
    }

    [Fact]
    public async Task Handle_MultipleAnnotations_ProcessesAll()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "dog", "#00FF00");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/1/img2.jpg", dataset.Id);
        var image3 = Image.Create("img3.jpg", "uploads/datasets/1/img3.jpg", dataset.Id);
        _context.Images.AddRange(image1, image2, image3);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat"", ""dog""]
            },
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 },
                { ""filename"": ""img2.jpg"", ""label"": 1, ""role"": 1 },
                { ""filename"": ""img3.jpg"", ""label"": 0, ""role"": 2 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(3);
        result.Value.FailedCount.Should().Be(0);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_RoleUpdate_UpdatesImageRole()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(class1);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat""]
            },
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.ImageId == image1.Id && r.ProjectId == project.Id);
        role.Should().NotBeNull();
        role!.RoleType.Value.Should().Be(1); // Train
    }

    [Fact]
    public async Task Handle_EmptyAnnotations_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat""]
            },
            ""annotations"": []
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task Handle_MissingHeader_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("type");
        result.Error.Should().Contain("missing");
    }

    [Fact]
    public async Task Handle_MissingCategories_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": []
            },
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Categories");
        result.Error.Should().Contain("missing");
    }

    [Fact]
    public async Task Handle_SegmentationPolygon_RequiresAtLeast3Points()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 2, "Test", dataset.Id, "user1"); // Segmentation
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "object", "#FF0000");
        _context.ProjectClasses.Add(class1);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        // Only 2 points - should be skipped
        var jsonContent = @"{
            ""header"": {
                ""type"": ""segmentation"",
                ""categories"": [""object""]
            },
            ""annotations"": [
                {
                    ""filename"": ""img1.jpg"",
                    ""label"": [[0, 10, 20, 30, 40]],
                    ""role"": 1
                }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SkippedCount.Should().Be(1);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty(); // Annotation not created due to insufficient points
    }

    [Fact]
    public async Task Handle_ExistingClassSameName_UsesExisting()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Pre-existing class
        var existingClass = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(existingClass);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image1);
        await _context.SaveChangesAsync();

        var jsonContent = @"{
            ""header"": {
                ""type"": ""classification"",
                ""categories"": [""cat""]
            },
            ""annotations"": [
                { ""filename"": ""img1.jpg"", ""label"": 0, ""role"": 1 }
            ]
        }";

        var command = new ImportAnnotationsCommand
        {
            ProjectId = project.Id,
            JsonContent = jsonContent
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(1);

        var classes = await _context.ProjectClasses.ToListAsync();
        classes.Should().HaveCount(1); // No new class created
        classes[0].Id.Should().Be(existingClass.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
