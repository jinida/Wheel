using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Annotations.Queries.GetAnnotationsByImage;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Annotations.Queries;

/// <summary>
/// Tests for GetAnnotationsByImageQueryHandler
/// Tests filtering by image, DTO mapping, and annotation data parsing
/// </summary>
public class GetAnnotationsByImageQueryHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly GetAnnotationsByImageQueryHandler _handler;
    private readonly IMapper _mapper;

    public GetAnnotationsByImageQueryHandlerTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);

        // Setup AutoMapper with real profiles
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AnnotationMappingProfile>();
            cfg.AddProfile<ProjectClassMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Setup repositories
        var annotationRepository = new AnnotationRepository(_context);

        _handler = new GetAnnotationsByImageQueryHandler(
            annotationRepository,
            _mapper);
    }

    [Fact]
    public async Task Handle_WithAnnotations_ReturnsAllAnnotationsForImage()
    {
        // Arrange - setup dataset, project, image, and annotations
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        // Create 3 annotations for the image
        for (int i = 0; i < 3; i++)
        {
            var annotation = Annotation.Create(
                image.Id,
                project.Id,
                projectClass.Id,
                "[[10,20],[30,40],[50,60]]");
            await _context.Annotations.AddAsync(annotation);
        }
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().OnlyContain(a => a.imageId == image.Id);
    }

    [Fact]
    public async Task Handle_FiltersByImage_OnlyReturnsMatchingAnnotations()
    {
        // Arrange - create multiple images with annotations
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "path/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "path/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        // Add annotations to image1
        for (int i = 0; i < 3; i++)
        {
            var annotation = Annotation.Create(image1.Id, project.Id, projectClass.Id, "[[10,20]]");
            await _context.Annotations.AddAsync(annotation);
        }

        // Add annotations to image2
        for (int i = 0; i < 2; i++)
        {
            var annotation = Annotation.Create(image2.Id, project.Id, projectClass.Id, "[[30,40]]");
            await _context.Annotations.AddAsync(annotation);
        }
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image1.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().OnlyContain(a => a.imageId == image1.Id);
    }

    [Fact]
    public async Task Handle_ImageWithNoAnnotations_ReturnsEmptyList()
    {
        // Arrange - create image without annotations
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonExistentImage_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAnnotationsByImageQuery { ImageId = 999 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ParsesNestedArrayFormat_Correctly()
    {
        // Arrange - test nested array format [[x1,y1],[x2,y2]]
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(
            image.Id,
            project.Id,
            projectClass.Id,
            "[[10.5,20.3],[30.1,40.7],[50.9,60.2]]");
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var annotationDto = result.Value[0];
        annotationDto.Information.Should().HaveCount(3);
        annotationDto.Information[0].X.Should().BeApproximately(10.5f, 0.01f);
        annotationDto.Information[0].Y.Should().BeApproximately(20.3f, 0.01f);
        annotationDto.Information[1].X.Should().BeApproximately(30.1f, 0.01f);
        annotationDto.Information[1].Y.Should().BeApproximately(40.7f, 0.01f);
    }

    [Fact]
    public async Task Handle_ParsesFlatArrayFormat_Correctly()
    {
        // Arrange - test flat array format [x1,y1,x2,y2]
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 1, "Person", "#00FF00");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(
            image.Id,
            project.Id,
            projectClass.Id,
            "[10,20,30,40,50,60]");
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var annotationDto = result.Value[0];
        annotationDto.Information.Should().HaveCount(3);
        annotationDto.Information[0].X.Should().Be(10);
        annotationDto.Information[0].Y.Should().Be(20);
        annotationDto.Information[1].X.Should().Be(30);
        annotationDto.Information[1].Y.Should().Be(40);
    }

    [Fact]
    public async Task Handle_MapsProjectClassDto_Correctly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 5, "Car", "#FF5500");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, "[[10,20]]");
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var annotationDto = result.Value[0];

        annotationDto.classDto.Should().NotBeNull();
        annotationDto.classDto.Name.Should().Be("Car");
        annotationDto.classDto.ClassIdx.Should().Be(5);
        annotationDto.classDto.Color.Should().Be("#FF5500");
    }

    [Fact]
    public async Task Handle_MultipleAnnotationsWithDifferentClasses_ReturnsAll()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var carClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        var personClass = ProjectClass.Create(project.Id, 1, "Person", "#00FF00");
        await _context.ProjectClasses.AddRangeAsync(carClass, personClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var carAnnotation = Annotation.Create(image.Id, project.Id, carClass.Id, "[[10,20]]");
        var personAnnotation = Annotation.Create(image.Id, project.Id, personClass.Id, "[[30,40]]");
        await _context.Annotations.AddRangeAsync(carAnnotation, personAnnotation);
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        result.Value.Should().ContainSingle(a => a.classDto.Name == "Car");
        result.Value.Should().ContainSingle(a => a.classDto.Name == "Person");
    }

    [Fact]
    public async Task Handle_InvalidJsonInformation_ReturnsEmptyPoints()
    {
        // Arrange - annotation with invalid JSON
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, "invalid json");
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Information.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsCreatedAt_Correctly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, "[[10,20]]");
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();

        var query = new GetAnnotationsByImageQuery { ImageId = image.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var annotationDto = result.Value[0];
        annotationDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
