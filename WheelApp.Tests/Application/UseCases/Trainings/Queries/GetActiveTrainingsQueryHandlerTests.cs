using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Trainings.Queries.GetActiveTrainings;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Trainings.Queries;

/// <summary>
/// Tests for GetActiveTrainingsQueryHandler
/// Tests filtering by active status, progress calculation, and DTO mapping
/// </summary>
public class GetActiveTrainingsQueryHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly GetActiveTrainingsQueryHandler _handler;
    private readonly IMapper _mapper;
    private readonly ITrainingProgressCalculator _progressCalculator;

    public GetActiveTrainingsQueryHandlerTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);

        // Setup AutoMapper with real profiles
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TrainingMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Setup mock progress calculator
        _progressCalculator = Substitute.For<ITrainingProgressCalculator>();
        _progressCalculator.CalculateProgress(Arg.Any<Training>()).Returns(50); // Default 50%

        // Setup repositories
        var trainingRepository = new TrainingRepository(_context);
        var logger = Substitute.For<ILogger<GetActiveTrainingsQueryHandler>>();

        _handler = new GetActiveTrainingsQueryHandler(
            trainingRepository,
            _mapper,
            _progressCalculator,
            logger);
    }

    [Fact]
    public async Task Handle_OnlyPendingTrainings_ReturnsAllPending()
    {
        // Arrange - create pending trainings
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 3; i++)
        {
            var training = Training.Start(project.Id, $"Training {i}");
            await _context.Trainings.AddAsync(training);
        }
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().OnlyContain(t => t.Status == TrainingStatus.Pending.Value);
    }

    [Fact]
    public async Task Handle_OnlyRunningTrainings_ReturnsAllRunning()
    {
        // Arrange - create running trainings
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 2; i++)
        {
            var training = Training.Start(project.Id, $"Training {i}");
            training.UpdateStatus(TrainingStatus.Running.Value);
            await _context.Trainings.AddAsync(training);
        }
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(t => t.Status == TrainingStatus.Running.Value);
    }

    [Fact]
    public async Task Handle_MixedPendingAndRunning_ReturnsAllActive()
    {
        // Arrange - create mix of pending and running trainings
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Segmentation.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Add pending trainings
        var pending1 = Training.Start(project.Id, "Pending 1");
        var pending2 = Training.Start(project.Id, "Pending 2");

        // Add running trainings
        var running1 = Training.Start(project.Id, "Running 1");
        running1.UpdateStatus(1); // Running
        var running2 = Training.Start(project.Id, "Running 2");
        running2.UpdateStatus(1); // Running

        await _context.Trainings.AddRangeAsync(pending1, pending2, running1, running2);
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.Should().Contain(t => t.Name == "Pending 1");
        result.Value.Should().Contain(t => t.Name == "Pending 2");
        result.Value.Should().Contain(t => t.Name == "Running 1");
        result.Value.Should().Contain(t => t.Name == "Running 2");
    }

    [Fact]
    public async Task Handle_ExcludesCompletedTrainings()
    {
        // Arrange - create mix of active and completed trainings
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var pending = Training.Start(project.Id, "Pending");
        var running = Training.Start(project.Id, "Running");
        running.UpdateStatus(1); // Running

        var completed = Training.Start(project.Id, "Completed");
        completed.UpdateStatus(1); // Running
        completed.Complete();

        await _context.Trainings.AddRangeAsync(pending, running, completed);
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().NotContain(t => t.Name == "Completed");
    }

    [Fact]
    public async Task Handle_ExcludesFailedTrainings()
    {
        // Arrange - create mix of active and failed trainings
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var pending = Training.Start(project.Id, "Pending");
        var running = Training.Start(project.Id, "Running");
        running.UpdateStatus(1); // Running

        var failed = Training.Start(project.Id, "Failed");
        failed.UpdateStatus(1); // Running
        failed.Fail("Error occurred");

        await _context.Trainings.AddRangeAsync(pending, running, failed);
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().NotContain(t => t.Name == "Failed");
    }

    [Fact]
    public async Task Handle_NoActiveTrainings_ReturnsEmptyList()
    {
        // Arrange - create only completed/failed trainings
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var completed = Training.Start(project.Id, "Completed");
        completed.UpdateStatus(1); // Running
        completed.Complete();

        await _context.Trainings.AddAsync(completed);
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - no trainings
        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CalculatesProgress_ForEachTraining()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training1 = Training.Start(project.Id, "Training 1");
        var training2 = Training.Start(project.Id, "Training 2");
        training2.UpdateStatus(1); // Running

        await _context.Trainings.AddRangeAsync(training1, training2);
        await _context.SaveChangesAsync();

        // Setup different progress for each training
        _progressCalculator.CalculateProgress(Arg.Is<Training>(t => t.Name == "Training 1")).Returns(10);
        _progressCalculator.CalculateProgress(Arg.Is<Training>(t => t.Name == "Training 2")).Returns(75);

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var training1Dto = result.Value.First(t => t.Name == "Training 1");
        training1Dto.Progress.Should().Be(10);

        var training2Dto = result.Value.First(t => t.Name == "Training 2");
        training2Dto.Progress.Should().Be(75);

        // Verify calculator was called for each training
        _progressCalculator.Received(2).CalculateProgress(Arg.Any<Training>());
    }

    [Fact]
    public async Task Handle_MapsDtoProperties_Correctly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "My Training");
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();

        _progressCalculator.CalculateProgress(Arg.Any<Training>()).Returns(33);

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];

        dto.Name.Should().Be("My Training");
        dto.ProjectId.Should().Be(project.Id);
        dto.Status.Should().Be(TrainingStatus.Pending.Value);
        dto.StatusName.Should().Be(TrainingStatus.Pending.Name);
        dto.Progress.Should().Be(33);
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RunningTraining_HasCorrectStatus()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Running Training");
        training.UpdateStatus(TrainingStatus.Running.Value);
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];

        dto.Status.Should().Be(TrainingStatus.Running.Value);
        dto.StatusName.Should().Be(TrainingStatus.Running.Name);
    }

    [Fact]
    public async Task Handle_MultipleProjects_ReturnsTrainingsFromAll()
    {
        // Arrange - create trainings from different projects
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        var project2 = Project.Create("Project 2", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        var training1 = Training.Start(project1.Id, "Training 1");
        var training2 = Training.Start(project2.Id, "Training 2");
        await _context.Trainings.AddRangeAsync(training1, training2);
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(t => t.ProjectId == project1.Id);
        result.Value.Should().Contain(t => t.ProjectId == project2.Id);
    }

    [Fact]
    public async Task Handle_OrderedByCreationDate()
    {
        // Arrange - create trainings at different times
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var training1 = Training.Start(project.Id, "First Training");
        await _context.Trainings.AddAsync(training1);
        await _context.SaveChangesAsync();

        await Task.Delay(10); // Small delay to ensure different timestamps

        var training2 = Training.Start(project.Id, "Second Training");
        await _context.Trainings.AddAsync(training2);
        await _context.SaveChangesAsync();

        var query = new GetActiveTrainingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        // Verify that creation dates are in ascending order (oldest first)
        var dates = result.Value.Select(t => t.CreatedAt).ToList();
        dates.Should().BeInAscendingOrder();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
