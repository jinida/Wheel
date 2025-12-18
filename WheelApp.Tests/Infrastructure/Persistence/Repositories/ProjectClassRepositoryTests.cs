using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

public class ProjectClassRepositoryTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly ProjectClassRepository _repository;

    public ProjectClassRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new ProjectClassRepository(_context);
    }

    [Fact]
    public async Task GetByProjectIdAsync_OrdersByClassIdx()
    {
        // CRITICAL: Cannot order by value object in SQL, orders in-memory
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var class3 = ProjectClass.Create(project.Id, 2, "Class 3", "#FF0000");
        var class1 = ProjectClass.Create(project.Id, 0, "Class 1", "#00FF00");
        var class2 = ProjectClass.Create(project.Id, 1, "Class 2", "#0000FF");
        // Add in random order
        await _context.ProjectClasses.AddRangeAsync(class3, class1, class2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        results.Should().HaveCount(3);
        results[0].ClassIdx.Value.Should().Be(0); // Ordered in memory
        results[1].ClassIdx.Value.Should().Be(1);
        results[2].ClassIdx.Value.Should().Be(2);
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

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByProjectIdAndClassIdxAsync_CompositeFilter()
    {
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

        // Act
        var result = await _repository.GetByProjectIdAndClassIdxAsync(project.Id, 0);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Person");
        result.ClassIdx.Value.Should().Be(0);
    }

    [Fact]
    public async Task GetByProjectIdAndClassIdxAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByProjectIdAndClassIdxAsync(999, 0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByProjectIdAndClassIdxAsync_WrongClassIdx_ReturnsNull()
    {
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

        // Act
        var result = await _repository.GetByProjectIdAndClassIdxAsync(project.Id, 1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByProjectIdAndClassIdxAsync_ReturnsAsNoTracking()
    {
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
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByProjectIdAndClassIdxAsync(project.Id, 0);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByIdsAsync_ValidIds_ReturnsProjectClasses()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "Car", "#00FF00");
        await _context.ProjectClasses.AddRangeAsync(class1, class2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByIdsAsync(new[] { class1.Id, class2.Id });

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdsAsync_EmptyList_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetByIdsAsync(Array.Empty<int>());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_NullList_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsAsNoTracking()
    {
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
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByIdsAsync(new[] { projectClass.Id });

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
