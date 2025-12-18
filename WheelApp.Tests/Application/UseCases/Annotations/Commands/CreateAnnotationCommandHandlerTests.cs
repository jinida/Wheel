using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Annotations.Commands.CreateAnnotation;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Annotations.Commands;

/// <summary>
/// Tests for CreateAnnotationCommandHandler
/// Tests annotation creation with validation of image, project, and class relationships
/// </summary>
public class CreateAnnotationCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly CreateAnnotationCommandHandler _handler;
    private readonly IMapper _mapper;

    public CreateAnnotationCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WheelAppDbContext(options);

        // Setup AutoMapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AnnotationMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Create real repositories
        var annotationRepository = new AnnotationRepository(_context);
        var imageRepository = new ImageRepository(_context);
        var projectRepository = new ProjectRepository(_context);
        var projectClassRepository = new ProjectClassRepository(_context);
        var unitOfWork = new UnitOfWork(_context);
        var logger = Substitute.For<ILogger<CreateAnnotationCommandHandler>>();

        _handler = new CreateAnnotationCommandHandler(
            annotationRepository,
            imageRepository,
            projectRepository,
            projectClassRepository,
            _mapper,
            unitOfWork,
            logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAnnotation()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "uploads/datasets/1/test.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var command = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = project.Id,
            ClassId = projectClass.Id,
            Information = "[[10,20],[100,200]]"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.imageId.Should().Be(image.Id);
        result.Value.classDto.Should().NotBeNull();
        result.Value.classDto.Id.Should().Be(projectClass.Id);

        var annotation = await _context.Annotations.FirstOrDefaultAsync();
        annotation.Should().NotBeNull();
        annotation!.ImageId.Should().Be(image.Id);
        annotation.ProjectId.Should().Be(project.Id);
        annotation.ClassId.Should().Be(projectClass.Id);
        annotation.Information.Should().Be("[[10,20],[100,200]]");
    }

    [Fact]
    public async Task Handle_ImageNotFound_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(projectClass);
        await _context.SaveChangesAsync();

        var command = new CreateAnnotationCommand
        {
            ImageId = 999,
            ProjectId = project.Id,
            ClassId = projectClass.Id,
            Information = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image");
        result.Error.Should().Contain("does not exist");

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "uploads/datasets/1/test.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var command = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = 999,
            ClassId = 1,
            Information = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project");
        result.Error.Should().Contain("does not exist");

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ClassNotFound_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "uploads/datasets/1/test.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var command = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = project.Id,
            ClassId = 999,
            Information = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Class");
        result.Error.Should().Contain("does not exist");

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ClassDoesNotBelongToProject_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 0, "Test", dataset.Id, "user1");
        var project2 = Project.Create("Project 2", 0, "Test", dataset.Id, "user1");
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        var classForProject2 = ProjectClass.Create(project2.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(classForProject2);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "uploads/datasets/1/test.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var command = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = project1.Id,
            ClassId = classForProject2.Id,
            Information = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("does not belong to project");

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullInformation_CreatesAnnotationSuccessfully()
    {
        // Arrange - Classification projects may not have coordinate information
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "uploads/datasets/1/test.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var command = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = project.Id,
            ClassId = projectClass.Id,
            Information = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var annotation = await _context.Annotations.FirstOrDefaultAsync();
        annotation.Should().NotBeNull();
        annotation!.Information.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MultipleAnnotationsForSameImage_CreatesAll()
    {
        // Arrange - Object detection can have multiple bounding boxes
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "person", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "car", "#00FF00");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "uploads/datasets/1/test.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var command1 = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = project.Id,
            ClassId = class1.Id,
            Information = "[[10,20],[100,200]]"
        };

        var command2 = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = project.Id,
            ClassId = class2.Id,
            Information = "[[50,60],[150,160]]"
        };

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().HaveCount(2);
        annotations.Should().OnlyContain(a => a.ImageId == image.Id);
        annotations.Should().OnlyContain(a => a.ProjectId == project.Id);
    }

    [Fact]
    public async Task Handle_SegmentationPolygon_CreatesAnnotationWithPolygonData()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 2, "Test", dataset.Id, "user1"); // Segmentation
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "object", "#FF0000");
        _context.ProjectClasses.Add(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "uploads/datasets/1/test.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var command = new CreateAnnotationCommand
        {
            ImageId = image.Id,
            ProjectId = project.Id,
            ClassId = projectClass.Id,
            Information = "[[10,20],[30,40],[50,60],[70,80]]" // Polygon with 4 points
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var annotation = await _context.Annotations.FirstOrDefaultAsync();
        annotation.Should().NotBeNull();
        annotation!.Information.Should().Contain("[10,20]");
        annotation.Information.Should().Contain("[30,40]");
        annotation.Information.Should().Contain("[50,60]");
        annotation.Information.Should().Contain("[70,80]");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
