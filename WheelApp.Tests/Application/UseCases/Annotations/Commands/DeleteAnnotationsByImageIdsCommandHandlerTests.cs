using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.UseCases.Annotations.Commands.DeleteAnnotationsByImageIds;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Annotations.Commands;

/// <summary>
/// Tests for DeleteAnnotationsByImageIdsCommandHandler
/// Tests bulk deletion of annotations by image IDs for performance optimization
/// </summary>
public class DeleteAnnotationsByImageIdsCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly DeleteAnnotationsByImageIdsCommandHandler _handler;

    public DeleteAnnotationsByImageIdsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WheelAppDbContext(options);

        // Create real repositories
        var annotationRepository = new AnnotationRepository(_context);
        var logger = Substitute.For<ILogger<DeleteAnnotationsByImageIdsCommandHandler>>();

        _handler = new DeleteAnnotationsByImageIdsCommandHandler(
            annotationRepository,
            logger);
    }

    [Fact]
    public async Task Handle_ValidImageIds_DeletesAllAnnotations()
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

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/1/img2.jpg", dataset.Id);
        _context.Images.AddRange(image1, image2);
        await _context.SaveChangesAsync();

        var annotation1 = Annotation.Create(image1.Id, project.Id, projectClass.Id, null);
        var annotation2 = Annotation.Create(image2.Id, project.Id, projectClass.Id, null);
        _context.Annotations.AddRange(annotation1, annotation2);
        await _context.SaveChangesAsync();

        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int> { image1.Id, image2.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyImageIdsList_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No image IDs provided");
    }

    [Fact]
    public async Task Handle_NoAnnotationsFound_ReturnsZero()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        // No annotations created

        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int> { image.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MultipleAnnotationsPerImage_DeletesAll()
    {
        // Arrange - Object detection can have multiple bounding boxes per image
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, "Test", dataset.Id, "user1"); // Object Detection
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "person", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "car", "#00FF00");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        var image = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var annotation1 = Annotation.Create(image.Id, project.Id, class1.Id, "[[10,20],[100,200]]");
        var annotation2 = Annotation.Create(image.Id, project.Id, class2.Id, "[[50,60],[150,160]]");
        var annotation3 = Annotation.Create(image.Id, project.Id, class1.Id, "[[30,40],[130,140]]");
        _context.Annotations.AddRange(annotation1, annotation2, annotation3);
        await _context.SaveChangesAsync();

        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int> { image.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PartialImageIds_DeletesOnlySpecifiedImages()
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

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/1/img2.jpg", dataset.Id);
        var image3 = Image.Create("img3.jpg", "uploads/datasets/1/img3.jpg", dataset.Id);
        _context.Images.AddRange(image1, image2, image3);
        await _context.SaveChangesAsync();

        var annotation1 = Annotation.Create(image1.Id, project.Id, projectClass.Id, null);
        var annotation2 = Annotation.Create(image2.Id, project.Id, projectClass.Id, null);
        var annotation3 = Annotation.Create(image3.Id, project.Id, projectClass.Id, null);
        _context.Annotations.AddRange(annotation1, annotation2, annotation3);
        await _context.SaveChangesAsync();

        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int> { image1.Id, image2.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        var remainingAnnotations = await _context.Annotations.ToListAsync();
        remainingAnnotations.Should().HaveCount(1);
        remainingAnnotations[0].ImageId.Should().Be(image3.Id);
    }

    [Fact]
    public async Task Handle_AnnotationsFromMultipleProjects_DeletesAll()
    {
        // Arrange - Same images can have annotations in multiple projects
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 0, "Test", dataset.Id, "user1");
        var project2 = Project.Create("Project 2", 0, "Test", dataset.Id, "user1");
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project1.Id, 0, "cat", "#FF0000");
        var class2 = ProjectClass.Create(project2.Id, 0, "dog", "#00FF00");
        _context.ProjectClasses.AddRange(class1, class2);
        await _context.SaveChangesAsync();

        var image = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var annotation1 = Annotation.Create(image.Id, project1.Id, class1.Id, null);
        var annotation2 = Annotation.Create(image.Id, project2.Id, class2.Id, null);
        _context.Annotations.AddRange(annotation1, annotation2);
        await _context.SaveChangesAsync();

        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int> { image.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonExistentImageIds_ReturnsZero()
    {
        // Arrange
        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int> { 999, 888, 777 }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MixOfExistingAndNonExistingImageIds_DeletesExistingOnes()
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

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/1/img2.jpg", dataset.Id);
        _context.Images.AddRange(image1, image2);
        await _context.SaveChangesAsync();

        var annotation1 = Annotation.Create(image1.Id, project.Id, projectClass.Id, null);
        var annotation2 = Annotation.Create(image2.Id, project.Id, projectClass.Id, null);
        _context.Annotations.AddRange(annotation1, annotation2);
        await _context.SaveChangesAsync();

        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = new List<int> { image1.Id, 999, image2.Id, 888 }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BulkDelete_PerformsBetterThanIndividualDeletes()
    {
        // Arrange - Performance test with many annotations
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "object", "#FF0000");
        _context.ProjectClasses.Add(projectClass);
        await _context.SaveChangesAsync();

        // Create 10 images with 5 annotations each = 50 annotations
        var imageIds = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            var image = Image.Create($"img{i}.jpg", $"uploads/datasets/1/img{i}.jpg", dataset.Id);
            _context.Images.Add(image);
            await _context.SaveChangesAsync();
            imageIds.Add(image.Id);

            for (int j = 0; j < 5; j++)
            {
                var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, $"[[{j},{j}],[{j + 10},{j + 10}]]");
                _context.Annotations.Add(annotation);
            }
        }
        await _context.SaveChangesAsync();

        var command = new DeleteAnnotationsByImageIdsCommand
        {
            ImageIds = imageIds
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(50);

        var annotations = await _context.Annotations.ToListAsync();
        annotations.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
