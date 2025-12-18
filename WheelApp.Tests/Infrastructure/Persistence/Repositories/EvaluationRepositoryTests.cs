using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

public class EvaluationRepositoryTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly EvaluationRepository _repository;

    public EvaluationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new EvaluationRepository(_context);
    }

    [Fact]
    public async Task GetByTrainingIdAsync_ReturnsAllEvaluations()
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

        var eval1 = Evaluation.Create(training.Id, "/eval/1.json", "{\"accuracy\": 0.90}");
        var eval2 = Evaluation.Create(training.Id, "/eval/2.json", "{\"accuracy\": 0.95}");
        await _context.Evaluations.AddRangeAsync(eval1, eval2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByTrainingIdAsync(training.Id);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(e => e.TrainingId == training.Id);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // AsNoTracking
    }

    [Fact]
    public async Task GetByTrainingIdAsync_EmptyTraining_ReturnsEmpty()
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

        // Act
        var results = await _repository.GetByTrainingIdAsync(training.Id);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTrainingIdAsync_NonExistentTraining_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetByTrainingIdAsync(999);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTrainingIdAsync_ReturnsAsNoTracking()
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

        var evaluation = Evaluation.Create(training.Id, "/eval/result.json", "{\"accuracy\": 0.95}");
        await _context.Evaluations.AddAsync(evaluation);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByTrainingIdAsync(training.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByTrainingIdAsync_MultipleTrainings_FiltersCorrectly()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training1 = Training.Start(project.Id, "Training 1");
        var training2 = Training.Start(project.Id, "Training 2");
        await _context.Trainings.AddRangeAsync(training1, training2);
        await _context.SaveChangesAsync();

        var eval1 = Evaluation.Create(training1.Id, "/eval/1.json", "{\"accuracy\": 0.90}");
        var eval2 = Evaluation.Create(training2.Id, "/eval/2.json", "{\"accuracy\": 0.95}");
        await _context.Evaluations.AddRangeAsync(eval1, eval2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByTrainingIdAsync(training1.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().TrainingId.Should().Be(training1.Id);
        results.Should().NotContain(e => e.TrainingId == training2.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
