using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

public class ProjectRepositoryTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly ProjectRepository _repository;

    public ProjectRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new ProjectRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAnnotationsAndClasses()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Classes.Should().HaveCount(1);
        result.Annotations.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByDatasetIdAsync_ReturnsAllProjects()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 0, null, dataset.Id, "user");
        var project2 = Project.Create("Project 2", 1, null, dataset.Id, "user");
        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByDatasetIdAsync(dataset.Id);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.DatasetId == dataset.Id);
    }

    [Fact]
    public async Task GetByDatasetIdAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Project", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByDatasetIdAsync(dataset.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByDatasetIdPagedAsync_ReturnsPaginatedProjects()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 15; i++)
        {
            var project = Project.Create($"Project {i}", 0, null, dataset.Id, "user");
            await _context.Projects.AddAsync(project);
        }
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetByDatasetIdPagedAsync(dataset.Id, pageNumber: 2, pageSize: 5);

        // Assert
        items.Should().HaveCount(5);
        totalCount.Should().Be(15);
    }

    [Fact]
    public async Task GetByDatasetIdPagedAsync_ConsistentOrdering()
    {
        // CRITICAL: Uses OrderBy(p => p.Id) for consistent pagination
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 15; i++)
        {
            var project = Project.Create($"Project {i}", 0, null, dataset.Id, "user");
            await _context.Projects.AddAsync(project);
        }
        await _context.SaveChangesAsync();

        // Act
        var (page1, _) = await _repository.GetByDatasetIdPagedAsync(dataset.Id, 1, 5);
        var (page2, _) = await _repository.GetByDatasetIdPagedAsync(dataset.Id, 2, 5);

        // Assert
        page1.Should().HaveCount(5);
        page2.Should().HaveCount(5);
        page1.Select(p => p.Id).Should().BeInAscendingOrder();
        page2.Select(p => p.Id).Should().BeInAscendingOrder();
        page1.Last().Id.Should().BeLessThan(page2.First().Id);
    }

    [Fact]
    public async Task GetExistingIdsAsync_ReturnsOnlyExistingIds()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 0, null, dataset.Id, "user");
        var project2 = Project.Create("Project 2", 1, null, dataset.Id, "user");
        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        // Act
        var existingIds = await _repository.GetExistingIdsAsync(new[] { project1.Id, project2.Id, 999 });

        // Assert
        existingIds.Should().HaveCount(2);
        existingIds.Should().Contain(project1.Id);
        existingIds.Should().Contain(project2.Id);
        existingIds.Should().NotContain(999);
    }

    [Fact]
    public async Task GetExistingIdsAsync_EmptyList_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetExistingIdsAsync(Array.Empty<int>());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingIdsAsync_NullList_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetExistingIdsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCountsByDatasetAsync_GroupByCount()
    {
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", null, "user");
        var dataset2 = Dataset.Create("Dataset 2", null, "user");
        await _context.Datasets.AddRangeAsync(dataset1, dataset2);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 3; i++)
        {
            await _context.Projects.AddAsync(Project.Create($"Proj{i}", 0, null, dataset1.Id, "user"));
        }
        for (int i = 0; i < 2; i++)
        {
            await _context.Projects.AddAsync(Project.Create($"Proj{i}", 0, null, dataset2.Id, "user"));
        }
        await _context.SaveChangesAsync();

        // Act
        var counts = await _repository.GetCountsByDatasetAsync();

        // Assert
        counts.Should().ContainKey(dataset1.Id);
        counts.Should().ContainKey(dataset2.Id);
        counts[dataset1.Id].Should().Be(3);
        counts[dataset2.Id].Should().Be(2);
    }

    [Fact]
    public async Task GetCountsByDatasetIdsAsync_OptimizedCount()
    {
        // CRITICAL: More efficient than GetCountsByDatasetAsync
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", null, "user");
        var dataset2 = Dataset.Create("Dataset 2", null, "user");
        var dataset3 = Dataset.Create("Dataset 3", null, "user");
        await _context.Datasets.AddRangeAsync(dataset1, dataset2, dataset3);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 3; i++)
        {
            await _context.Projects.AddAsync(Project.Create($"Proj{i}", 0, null, dataset1.Id, "user"));
        }
        for (int i = 0; i < 2; i++)
        {
            await _context.Projects.AddAsync(Project.Create($"Proj{i}", 0, null, dataset2.Id, "user"));
        }
        await _context.SaveChangesAsync();

        // Act
        var counts = await _repository.GetCountsByDatasetIdsAsync(new List<int> { dataset1.Id, dataset2.Id });

        // Assert
        counts.Should().ContainKey(dataset1.Id);
        counts.Should().ContainKey(dataset2.Id);
        counts.Should().NotContainKey(dataset3.Id); // Not queried
        counts[dataset1.Id].Should().Be(3);
        counts[dataset2.Id].Should().Be(2);
    }

    [Fact]
    public async Task GetWithDetailsAsync_IncludesMultipleNavigations()
    {
        // CRITICAL: Includes Classes, Annotations, Trainings, Dataset
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var training = Training.Start(project.Id, "Training 1");
        await _context.Trainings.AddAsync(training);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetWithDetailsAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Classes.Should().HaveCount(1);
        result.Annotations.Should().NotBeNull();
        result.Trainings.Should().HaveCount(1);
        result.Dataset.Should().NotBeNull();
        result.Dataset!.Name.Value.Should().Be("Test");
    }

    [Fact]
    public async Task GetWithDetailsAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repository.GetWithDetailsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithDetailsAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetWithDetailsAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByTypeAsync_FiltersByValueObjectValue()
    {
        // CRITICAL: Filters by Type.Value (int)
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Classification", 0, null, dataset.Id, "user");
        var project2 = Project.Create("Detection", 1, null, dataset.Id, "user");
        var project3 = Project.Create("Segmentation", 2, null, dataset.Id, "user");
        await _context.Projects.AddRangeAsync(project1, project2, project3);
        await _context.SaveChangesAsync();

        // Act
        var type = ProjectType.FromValue(0); // Classification
        var results = await _repository.GetByTypeAsync(type);

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
        results.Should().OnlyContain(p => p.Type.Value == 0);
    }

    [Fact]
    public async Task GetByTypeAsync_NullType_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetByTypeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByTypeAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Classification", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var type = ProjectType.FromValue(0);
        var results = await _repository.GetByTypeAsync(type);

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
