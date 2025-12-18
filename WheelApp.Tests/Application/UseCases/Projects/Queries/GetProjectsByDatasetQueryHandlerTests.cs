using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Projects.Queries.GetProjectsByDataset;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Projects.Queries;

/// <summary>
/// Tests for GetProjectsByDatasetQueryHandler
/// Tests pagination, filtering by dataset, and DTO mapping
/// </summary>
public class GetProjectsByDatasetQueryHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly GetProjectsByDatasetQueryHandler _handler;
    private readonly IMapper _mapper;

    public GetProjectsByDatasetQueryHandlerTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);

        // Setup AutoMapper with real profiles
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProjectMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Setup repositories
        var projectRepository = new ProjectRepository(_context);
        var logger = Substitute.For<ILogger<GetProjectsByDatasetQueryHandler>>();

        _handler = new GetProjectsByDatasetQueryHandler(
            projectRepository,
            _mapper,
            logger);
    }

    [Fact]
    public async Task Handle_WithProjects_ReturnsPagedResults()
    {
        // Arrange - create dataset with projects
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 15; i++)
        {
            var project = Project.Create(
                $"Project {i}",
                ProjectType.Classification,
                null,
                dataset.Id,
                "test-user");
            await _context.Projects.AddAsync(project);
        }
        await _context.SaveChangesAsync();

        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = dataset.Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(15);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SecondPage_ReturnsCorrectPage()
    {
        // Arrange - create dataset with projects
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 15; i++)
        {
            var project = Project.Create(
                $"Project {i}",
                ProjectType.ObjectDetection,
                null,
                dataset.Id,
                "test-user");
            await _context.Projects.AddAsync(project);
        }
        await _context.SaveChangesAsync();

        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = dataset.Id,
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(15);
    }

    [Fact]
    public async Task Handle_FiltersByDataset_OnlyReturnsMatchingProjects()
    {
        // Arrange - create multiple datasets with projects
        var dataset1 = Dataset.Create("Dataset 1", null, "test-user");
        var dataset2 = Dataset.Create("Dataset 2", null, "test-user");
        await _context.Datasets.AddRangeAsync(dataset1, dataset2);
        await _context.SaveChangesAsync();

        // Add projects to dataset 1
        for (int i = 1; i <= 5; i++)
        {
            var project = Project.Create(
                $"Dataset1 Project {i}",
                ProjectType.Classification,
                null,
                dataset1.Id,
                "test-user");
            await _context.Projects.AddAsync(project);
        }

        // Add projects to dataset 2
        for (int i = 1; i <= 3; i++)
        {
            var project = Project.Create(
                $"Dataset2 Project {i}",
                ProjectType.Segmentation,
                null,
                dataset2.Id,
                "test-user");
            await _context.Projects.AddAsync(project);
        }
        await _context.SaveChangesAsync();

        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = dataset1.Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Items.Should().OnlyContain(p => p.DatasetId == dataset1.Id);
        result.Value.Items.Should().OnlyContain(p => p.Name.StartsWith("Dataset1"));
    }

    [Fact]
    public async Task Handle_DatasetWithNoProjects_ReturnsEmptyResult()
    {
        // Arrange - create dataset without projects
        var dataset = Dataset.Create("Empty Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = dataset.Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NonExistentDataset_ReturnsEmptyResult()
    {
        // Arrange - query for dataset that doesn't exist
        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = 999,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InvalidPageNumber_ReturnsFailure()
    {
        // Arrange
        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = 1,
            PageNumber = 0,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Page number must be greater than 0");
    }

    [Fact]
    public async Task Handle_InvalidPageSize_TooSmall_ReturnsFailure()
    {
        // Arrange
        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = 1,
            PageNumber = 1,
            PageSize = 0
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task Handle_InvalidPageSize_TooLarge_ReturnsFailure()
    {
        // Arrange
        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = 1,
            PageNumber = 1,
            PageSize = 101
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task Handle_MapsAllDtoProperties_Correctly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create(
            "Test Project",
            ProjectType.ObjectDetection,
            "Test Description",
            dataset.Id,
            "john.doe");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = dataset.Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];

        dto.Name.Should().Be("Test Project");
        dto.Description.Should().Be("Test Description");
        dto.TypeName.Should().Be("Object Detection");
        dto.DatasetId.Should().Be(dataset.Id);
        dto.CreatedBy.Should().Be("john.doe");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_DifferentProjectTypes_MapsTypesCorrectly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var classificationProject = Project.Create("Classification Project", ProjectType.Classification, null, dataset.Id, "test-user");
        var detectionProject = Project.Create("Detection Project", ProjectType.ObjectDetection, null, dataset.Id, "test-user");
        var segmentationProject = Project.Create("Segmentation Project", ProjectType.Segmentation, null, dataset.Id, "test-user");
        var anomalyProject = Project.Create("Anomaly Project", ProjectType.AnomalyDetection, null, dataset.Id, "test-user");

        await _context.Projects.AddRangeAsync(classificationProject, detectionProject, segmentationProject, anomalyProject);
        await _context.SaveChangesAsync();

        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = dataset.Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(4);

        result.Value.Items.Should().ContainSingle(p => p.TypeName == "Classification");
        result.Value.Items.Should().ContainSingle(p => p.TypeName == "Object Detection");
        result.Value.Items.Should().ContainSingle(p => p.TypeName == "Segmentation");
        result.Value.Items.Should().ContainSingle(p => p.TypeName == "Anomaly Detection");
    }

    [Fact]
    public async Task Handle_CustomPageSize_ReturnsCorrectCount()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 30; i++)
        {
            var project = Project.Create(
                $"Project {i}",
                ProjectType.Classification,
                null,
                dataset.Id,
                "test-user");
            await _context.Projects.AddAsync(project);
        }
        await _context.SaveChangesAsync();

        var query = new GetProjectsByDatasetQuery
        {
            DatasetId = dataset.Id,
            PageNumber = 1,
            PageSize = 15
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(15);
        result.Value.TotalCount.Should().Be(30);
        result.Value.TotalPages.Should().Be(2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
