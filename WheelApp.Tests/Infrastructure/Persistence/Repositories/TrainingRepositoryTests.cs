using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

public class TrainingRepositoryTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly TrainingRepository _repository;

    public TrainingRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new TrainingRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_IncludesProjectAndDataset()
    {
        // CRITICAL: ThenInclude for nested navigation
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Training 1");
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
        var testTraining = results.First(t => t.Id == training.Id);
        testTraining.Project.Should().NotBeNull();
        testTraining.Project!.Dataset.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Training 1");
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByProjectIdAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training1 = Training.Start(project.Id, "Training 1");
        await _context.Trainings.AddAsync(training1);
        await _context.SaveChangesAsync();
        await Task.Delay(10); // Ensure different timestamps

        var training2 = Training.Start(project.Id, "Training 2");
        await _context.Trainings.AddAsync(training2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        results.Should().HaveCount(2);
        results[0].Id.Should().Be(training2.Id); // Most recent first
        results[1].Id.Should().Be(training1.Id);
    }

    [Fact]
    public async Task GetByProjectIdAsync_EmptyProject_ReturnsEmpty()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Training 1");
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetWithEvaluationsAsync_IncludesEvaluations()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Training 1");
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();

        var evaluation = Evaluation.Create(training.Id, "/eval/result.json", "{\"accuracy\": 0.95}");
        await _context.Evaluations.AddAsync(evaluation);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetWithEvaluationsAsync(training.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Evaluations.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWithEvaluationsAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repository.GetWithEvaluationsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithEvaluationsAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Training 1");
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetWithEvaluationsAsync(training.Id);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetActiveTrainingsAsync_ReturnsOnlyRunningStatus()
    {
        // CRITICAL: Filters by Running status
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training1 = Training.Start(project.Id, "Running");
        training1.UpdateStatus(1); // Running
        var training2 = Training.Start(project.Id, "Completed");
        training2.Complete(); // Completed
        await _context.Trainings.AddRangeAsync(training1, training2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetActiveTrainingsAsync();

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
        results.Should().OnlyContain(t => t.Status.Value == 1); // Running
    }

    [Fact]
    public async Task GetActiveTrainingsAsync_NoActiveTrainings_ReturnsEmpty()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Completed");
        training.Complete(); // Completed
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetActiveTrainingsAsync();

        // Assert
        // Should not contain our completed training
        results.Should().NotContain(t => t.Id == training.Id);
    }

    [Fact]
    public async Task GetActiveTrainingsAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Running");
        training.UpdateStatus(1); // Running
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetActiveTrainingsAsync();

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
