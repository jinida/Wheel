using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Datasets.Queries.GetDatasets;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Datasets.Queries;

/// <summary>
/// Tests for GetDatasetsQueryHandler
/// Tests pagination, DTO mapping, and image/project count aggregation
/// </summary>
public class GetDatasetsQueryHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly GetDatasetsQueryHandler _handler;
    private readonly IMapper _mapper;

    public GetDatasetsQueryHandlerTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);

        // Setup AutoMapper with real profiles
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DatasetMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Setup repositories
        var datasetRepository = new DatasetRepository(_context);
        var imageRepository = new ImageRepository(_context);
        var projectRepository = new ProjectRepository(_context);
        var logger = Substitute.For<ILogger<GetDatasetsQueryHandler>>();

        _handler = new GetDatasetsQueryHandler(
            datasetRepository,
            imageRepository,
            projectRepository,
            _mapper,
            logger);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedResults()
    {
        // Arrange - create 25 datasets
        for (int i = 1; i <= 25; i++)
        {
            var dataset = Dataset.Create($"Dataset {i}", $"Description {i}", "test-user");
            await _context.Datasets.AddAsync(dataset);
        }
        await _context.SaveChangesAsync();

        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_SecondPage_ReturnsCorrectPage()
    {
        // Arrange - create 25 datasets
        for (int i = 1; i <= 25; i++)
        {
            var dataset = Dataset.Create($"Dataset {i}", $"Description {i}", "test-user");
            await _context.Datasets.AddAsync(dataset);
        }
        await _context.SaveChangesAsync();

        var query = new GetDatasetsQuery { PageNumber = 2, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_LastPage_ReturnsRemainingItems()
    {
        // Arrange - create 25 datasets
        for (int i = 1; i <= 25; i++)
        {
            var dataset = Dataset.Create($"Dataset {i}", null, "test-user");
            await _context.Datasets.AddAsync(dataset);
        }
        await _context.SaveChangesAsync();

        var query = new GetDatasetsQuery { PageNumber = 3, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5); // Only 5 items on last page
        result.Value.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange - no datasets
        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithImages_ReturnsCorrectImageCounts()
    {
        // Arrange - create datasets with images
        var dataset1 = Dataset.Create("Dataset 1", null, "test-user");
        var dataset2 = Dataset.Create("Dataset 2", null, "test-user");
        await _context.Datasets.AddRangeAsync(dataset1, dataset2);
        await _context.SaveChangesAsync();

        // Add images to dataset 1
        for (int i = 0; i < 5; i++)
        {
            var image = Image.Create($"image{i}.jpg", $"path/image{i}.jpg", dataset1.Id);
            await _context.Images.AddAsync(image);
        }

        // Add images to dataset 2
        for (int i = 0; i < 3; i++)
        {
            var image = Image.Create($"image{i}.jpg", $"path/image{i}.jpg", dataset2.Id);
            await _context.Images.AddAsync(image);
        }
        await _context.SaveChangesAsync();

        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        var dataset1Dto = result.Value.Items.First(d => d.Name == "Dataset 1");
        var dataset2Dto = result.Value.Items.First(d => d.Name == "Dataset 2");

        dataset1Dto.ImageCount.Should().Be(5);
        dataset2Dto.ImageCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithoutImages_ReturnsZeroImageCount()
    {
        // Arrange - create dataset without images
        var dataset = Dataset.Create("Empty Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ImageCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InvalidPageNumber_ReturnsFailure()
    {
        // Arrange
        var query = new GetDatasetsQuery { PageNumber = 0, PageSize = 10 };

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
        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 0 };

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
        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 101 };

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
        var dataset = Dataset.Create("Test Dataset", "Test Description", "john.doe");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];

        dto.Name.Should().Be("Test Dataset");
        dto.Description.Should().Be("Test Description");
        dto.CreatedBy.Should().Be("john.doe");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.ModifiedAt.Should().BeNull();
        dto.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PageBeyondResults_ReturnsEmptyPage()
    {
        // Arrange - create only 5 datasets
        for (int i = 1; i <= 5; i++)
        {
            var dataset = Dataset.Create($"Dataset {i}", null, "test-user");
            await _context.Datasets.AddAsync(dataset);
        }
        await _context.SaveChangesAsync();

        // Request page 10
        var query = new GetDatasetsQuery { PageNumber = 10, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_CustomPageSize_ReturnsCorrectCount()
    {
        // Arrange
        for (int i = 1; i <= 50; i++)
        {
            var dataset = Dataset.Create($"Dataset {i}", null, "test-user");
            await _context.Datasets.AddAsync(dataset);
        }
        await _context.SaveChangesAsync();

        var query = new GetDatasetsQuery { PageNumber = 1, PageSize = 25 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(25);
        result.Value.TotalCount.Should().Be(50);
        result.Value.TotalPages.Should().Be(2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
