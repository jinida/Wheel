using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.UseCases.Datasets.Commands.DeleteDatasets;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Datasets.Commands;

/// <summary>
/// Tests for DeleteDatasetsCommandHandler
/// CRITICAL: Tests cascade delete behavior - deleting dataset must delete all related data
/// </summary>
public class DeleteDatasetsCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly DeleteDatasetsCommandHandler _handler;

    public DeleteDatasetsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WheelAppDbContext(options);

        // Create real repositories
        var datasetRepository = new DatasetRepository(_context);
        var projectRepository = new ProjectRepository(_context);
        var logger = Substitute.For<ILogger<DeleteDatasetsCommandHandler>>();

        _handler = new DeleteDatasetsCommandHandler(
            datasetRepository,
            projectRepository,
            logger);
    }

    [Fact]
    public async Task Handle_ValidId_DeletesDataset()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { dataset.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        var datasets = await _context.Datasets.ToListAsync();
        datasets.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DatasetNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { 999 }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_EmptyIdList_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No dataset IDs provided");
    }

    [Fact]
    public async Task Handle_MultipleDatasets_DeletesAll()
    {
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", "Test", "user1");
        var dataset2 = Dataset.Create("Dataset 2", "Test", "user1");
        var dataset3 = Dataset.Create("Dataset 3", "Test", "user1");
        _context.Datasets.AddRange(dataset1, dataset2, dataset3);
        await _context.SaveChangesAsync();

        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { dataset1.Id, dataset2.Id, dataset3.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);

        var datasets = await _context.Datasets.ToListAsync();
        datasets.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CascadeDelete_DeletesImagesAndProjects()
    {
        // Arrange - CRITICAL: Test cascade delete
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        // Add images to dataset
        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/1/img2.jpg", dataset.Id);
        _context.Images.AddRange(image1, image2);
        await _context.SaveChangesAsync();

        // Add project to dataset
        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { dataset.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        var datasets = await _context.Datasets.ToListAsync();
        datasets.Should().BeEmpty();

        // Verify images are deleted (cascade)
        var images = await _context.Images.ToListAsync();
        images.Should().BeEmpty();

        // Verify projects are deleted
        var projects = await _context.Projects.ToListAsync();
        projects.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CascadeDelete_DeletesProjectWithAllRelatedData()
    {
        // Arrange - CRITICAL: Test full cascade delete chain
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Add project class
        var projectClass = ProjectClass.Create(project.Id, 0, "cat", "#FF0000");
        _context.ProjectClasses.Add(projectClass);
        await _context.SaveChangesAsync();

        // Add annotation
        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        _context.Annotations.Add(annotation);
        await _context.SaveChangesAsync();

        // Add role
        var role = Role.Create(image.Id, project.Id, 1);
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Add training
        var training = Training.Start(project.Id, "Training 1");
        _context.Trainings.Add(training);
        await _context.SaveChangesAsync();

        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { dataset.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify everything is deleted
        (await _context.Datasets.ToListAsync()).Should().BeEmpty();
        (await _context.Images.ToListAsync()).Should().BeEmpty();
        (await _context.Projects.ToListAsync()).Should().BeEmpty();
        (await _context.ProjectClasses.ToListAsync()).Should().BeEmpty();
        (await _context.Annotations.ToListAsync()).Should().BeEmpty();
        (await _context.Roles.ToListAsync()).Should().BeEmpty();
        (await _context.Trainings.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PartialFailure_DeletesSuccessfulOnesAndReportsErrors()
    {
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", "Test", "user1");
        var dataset2 = Dataset.Create("Dataset 2", "Test", "user1");
        _context.Datasets.AddRange(dataset1, dataset2);
        await _context.SaveChangesAsync();

        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { dataset1.Id, 999, dataset2.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Deleted 2 of 3");
        result.Error.Should().Contain("not found");

        var datasets = await _context.Datasets.ToListAsync();
        datasets.Should().BeEmpty(); // Both valid datasets were deleted
    }

    [Fact]
    public async Task Handle_DeleteDatasetWithMultipleProjects_DeletesAllProjects()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 0, "Test", dataset.Id, "user1");
        var project2 = Project.Create("Project 2", 1, "Test", dataset.Id, "user1");
        var project3 = Project.Create("Project 3", 2, "Test", dataset.Id, "user1");
        _context.Projects.AddRange(project1, project2, project3);
        await _context.SaveChangesAsync();

        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { dataset.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        var datasets = await _context.Datasets.ToListAsync();
        datasets.Should().BeEmpty();

        var projects = await _context.Projects.ToListAsync();
        projects.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeleteOneDataset_LeavesOtherDatasetsIntact()
    {
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", "Test", "user1");
        var dataset2 = Dataset.Create("Dataset 2", "Test", "user1");
        _context.Datasets.AddRange(dataset1, dataset2);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset1.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/2/img2.jpg", dataset2.Id);
        _context.Images.AddRange(image1, image2);
        await _context.SaveChangesAsync();

        var command = new DeleteDatasetsCommand
        {
            Ids = new List<int> { dataset1.Id }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        var datasets = await _context.Datasets.ToListAsync();
        datasets.Should().HaveCount(1);
        datasets[0].Id.Should().Be(dataset2.Id);

        var images = await _context.Images.ToListAsync();
        images.Should().HaveCount(1);
        images[0].Id.Should().Be(image2.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
