using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

/// <summary>
/// Tests for RepositoryBase generic CRUD operations using InMemory database
/// Tests the base implementation that all repositories inherit from
/// </summary>
public class RepositoryBaseTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly DatasetRepository _repository;

    public RepositoryBaseTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new DatasetRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(dataset.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(dataset.Id);
        result.Name.Value.Should().Be("Test Dataset");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentEntity_ReturnsNull()
    {
        // Arrange
        // Empty database

        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(1);
        // Verify AsNoTracking by checking ChangeTracker
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        // Empty database

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsAddedEntity()
    {
        // Arrange
        var dataset = Dataset.Create("New Dataset", null, "user");

        // Act
        var result = await _repository.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        _context.Datasets.Should().Contain(d => d.Id == result.Id);
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        Dataset? nullDataset = null;

        // Act
        var act = async () => await _repository.AddAsync(nullDataset!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddRangeAsync_MultipleEntities_AddsAll()
    {
        // Arrange
        var datasets = new[]
        {
            Dataset.Create("Dataset 1", null, "user"),
            Dataset.Create("Dataset 2", null, "user"),
            Dataset.Create("Dataset 3", null, "user")
        };

        // Act
        await _repository.AddRangeAsync(datasets);
        await _context.SaveChangesAsync();

        // Assert
        var all = await _repository.GetAllAsync();
        all.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task AddRangeAsync_EmptyCollection_DoesNothing()
    {
        // Arrange
        var emptyList = Array.Empty<Dataset>();

        // Act
        await _repository.AddRangeAsync(emptyList);
        await _context.SaveChangesAsync();

        // Assert
        var all = await _repository.GetAllAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRangeAsync_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Dataset>? nullCollection = null;

        // Act
        var act = async () => await _repository.AddRangeAsync(nullCollection!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_ModifiedEntity_MarksAsModified()
    {
        // Arrange
        var dataset = Dataset.Create("Original", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        dataset.UpdateName("Updated", "modifier");

        // Act
        await _repository.UpdateAsync(dataset);

        // Assert
        var entry = _context.Entry(dataset);
        entry.State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        Dataset? nullDataset = null;

        // Act
        var act = async () => await _repository.UpdateAsync(nullDataset!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteAsync_TrackedEntity_RemovesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("To Delete", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(dataset);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(dataset.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_UntrackedEntity_RemovesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("To Delete", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); // Detach entity

        // Act
        await _repository.DeleteAsync(dataset);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(dataset.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        Dataset? nullDataset = null;

        // Act
        var act = async () => await _repository.DeleteAsync(nullDataset!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteByIdAsync_UsesExecuteSqlInterpolated()
    {
        // CRITICAL: DeleteByIdAsync uses raw SQL for efficiency
        // Arrange
        var dataset = Dataset.Create("To Delete By ID", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();
        var id = dataset.Id;

        // Act
        await _repository.DeleteByIdAsync(id);

        // Assert
        var result = await _context.Datasets.FindAsync(id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByIdAsync_TrackedEntity_MarksAsDeleted()
    {
        // Arrange
        var dataset = Dataset.Create("To Delete By ID", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();
        var id = dataset.Id;
        // Entity is still tracked from SaveChangesAsync

        // Act
        await _repository.DeleteByIdAsync(id);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Datasets.FindAsync(id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRangeAsync_MultipleEntities_DeletesAll()
    {
        // Arrange
        var datasets = new[]
        {
            Dataset.Create("Delete 1", null, "user"),
            Dataset.Create("Delete 2", null, "user")
        };
        await _context.Datasets.AddRangeAsync(datasets);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteRangeAsync(datasets);
        await _context.SaveChangesAsync();

        // Assert
        var remaining = await _repository.GetAllAsync();
        remaining.Should().NotContain(datasets);
    }

    [Fact]
    public async Task DeleteRangeAsync_EmptyCollection_DoesNothing()
    {
        // Arrange
        var dataset = Dataset.Create("Keep", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteRangeAsync(Array.Empty<Dataset>());
        await _context.SaveChangesAsync();

        // Assert
        var all = await _repository.GetAllAsync();
        all.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteRangeAsync_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Dataset>? nullCollection = null;

        // Act
        var act = async () => await _repository.DeleteRangeAsync(nullCollection!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Detach_ClearsChangeTracker()
    {
        // Arrange
        var dataset = Dataset.Create("Detach Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Act
        _repository.Detach(dataset);

        // Assert
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public void Detach_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        Dataset? nullDataset = null;

        // Act
        var act = () => _repository.Detach(nullDataset!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var dataset = Dataset.Create("Exists Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(dataset.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        // Empty database

        // Act
        var exists = await _repository.ExistsAsync(999);

        // Assert
        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
