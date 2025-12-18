using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

public class RoleRepositoryTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly RoleRepository _repository;

    public RoleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new RoleRepository(_context);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role = Role.Create(image.Id, project.Id, 1); // Train
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByProjectIdTrackingAsync_ReturnsWithTracking()
    {
        // CRITICAL: For update operations
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role = Role.Create(image.Id, project.Id, 1);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByProjectIdTrackingAsync(project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().NotBeEmpty(); // WITH tracking
    }

    [Fact]
    public async Task GetByIdsAsync_ValidIds_ReturnsRoles()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role1 = Role.Create(image1.Id, project.Id, 1);
        var role2 = Role.Create(image2.Id, project.Id, 2);
        await _context.Roles.AddRangeAsync(role1, role2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByIdsAsync(new[] { role1.Id, role2.Id });

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
    public async Task GetByImageIdsAsync_PreventNPlusOne_BatchQuery()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role1 = Role.Create(image1.Id, project.Id, 1);
        var role2 = Role.Create(image2.Id, project.Id, 1);
        await _context.Roles.AddRangeAsync(role1, role2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByImageIdsAsync(new[] { image1.Id, image2.Id });

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.ImageId == image1.Id);
        results.Should().Contain(r => r.ImageId == image2.Id);
    }

    [Fact]
    public async Task GetByImageIdsAsync_EmptyList_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetByImageIdsAsync(Array.Empty<int>());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByImageIdsAsync_NullList_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetByImageIdsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByImageAndProjectAsync_ExistingRole_ReturnsRole()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role = Role.Create(image.Id, project.Id, 1);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByImageAndProjectAsync(image.Id, project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ImageId.Should().Be(image.Id);
        result.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task GetByImageAndProjectAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByImageAndProjectAsync(999, 999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByRoleTypeAsync_FiltersByValueObjectValue()
    {
        // CRITICAL: Filters by RoleType.Value (int)
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role1 = Role.Create(image1.Id, project.Id, 1); // Train
        var role2 = Role.Create(image2.Id, project.Id, 2); // Val
        await _context.Roles.AddRangeAsync(role1, role2);
        await _context.SaveChangesAsync();

        // Act
        var roleType = RoleType.FromValue(1); // Train
        var results = await _repository.GetByRoleTypeAsync(project.Id, roleType);

        // Assert
        results.Should().HaveCount(1);
        results.First().RoleType.Value.Should().Be(1);
    }

    [Fact]
    public async Task GetByRoleTypeAsync_NullRoleType_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetByRoleTypeAsync(1, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BatchUpdateAsync_UsesUpdateRange()
    {
        // CRITICAL: Batch update operation
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role1 = Role.Create(image1.Id, project.Id, 1); // Train
        var role2 = Role.Create(image2.Id, project.Id, 1); // Train
        await _context.Roles.AddRangeAsync(role1, role2);
        await _context.SaveChangesAsync();

        role1.ChangeRole(2); // Change to Val
        role2.ChangeRole(3); // Change to Test

        // Act
        var count = await _repository.BatchUpdateAsync(new[] { role1, role2 });

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task BatchUpdateAsync_EmptyList_ReturnsZero()
    {
        // Act
        var count = await _repository.BatchUpdateAsync(Array.Empty<Role>());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task BatchUpdateAsync_NullList_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.BatchUpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BatchDeleteAsync_FetchesThenRemoves()
    {
        // CRITICAL: Fetches entities first, then removes
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var role1 = Role.Create(image1.Id, project.Id, 1);
        var role2 = Role.Create(image2.Id, project.Id, 1);
        await _context.Roles.AddRangeAsync(role1, role2);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.BatchDeleteAsync(new[] { role1.Id, role2.Id });
        await _context.SaveChangesAsync();

        // Assert
        count.Should().Be(2);
        var remaining = await _repository.GetByProjectIdAsync(project.Id);
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchDeleteAsync_EmptyList_ReturnsZero()
    {
        // Act
        var count = await _repository.BatchDeleteAsync(Array.Empty<int>());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task BatchDeleteAsync_NullList_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.BatchDeleteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
