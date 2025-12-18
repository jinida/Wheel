using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

public class AnnotationRepositoryTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly AnnotationRepository _repository;

    public AnnotationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new AnnotationRepository(_context);
    }

    [Fact]
    public async Task GetByImageIdAsync_ImageWithAnnotations_ReturnsAll()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var annotation1 = Annotation.Create(image.Id, project.Id, projectClass.Id, "[10, 10, 100, 100]");
        var annotation2 = Annotation.Create(image.Id, project.Id, projectClass.Id, "[20, 20, 120, 120]");
        await _context.Annotations.AddRangeAsync(annotation1, annotation2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByImageIdAsync(image.Id);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(a => a.ImageId == image.Id);
    }

    [Fact]
    public async Task GetByImageIdAsync_ReturnsAsNoTracking()
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

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByImageIdAsync(image.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByImageIdsAsync_PreventNPlusOne_BatchQuery()
    {
        // CRITICAL: Prevents N+1 query problem
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var ann1 = Annotation.Create(image1.Id, project.Id, projectClass.Id, null);
        var ann2 = Annotation.Create(image2.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddRangeAsync(ann1, ann2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByImageIdsAsync(new[] { image1.Id, image2.Id });

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(a => a.ImageId == image1.Id);
        results.Should().Contain(a => a.ImageId == image2.Id);
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
    public async Task GetByProjectIdAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByProjectIdTrackedAsync_ReturnsWithTracking()
    {
        // CRITICAL: For delete operations that need tracking
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByProjectIdTrackedAsync(project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().NotBeEmpty(); // WITH tracking
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

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByProjectIdTrackingAsync(project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().NotBeEmpty(); // WITH tracking
    }

    [Fact]
    public async Task GetByImageAndProjectAsync_ExistingAnnotation_ReturnsAnnotation()
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

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddAsync(annotation);
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
    public async Task GetByIdsAsync_ValidIds_ReturnsAnnotations()
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

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var ann1 = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        var ann2 = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddRangeAsync(ann1, ann2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByIdsAsync(new[] { ann1.Id, ann2.Id });

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
    public async Task UpdateRangeAsync_BatchUpdate_ReturnsCount()
    {
        // CRITICAL: Batch update operation
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var ann1 = Annotation.Create(image.Id, project.Id, projectClass.Id, "[10,10,50,50]");
        var ann2 = Annotation.Create(image.Id, project.Id, projectClass.Id, "[20,20,60,60]");
        await _context.Annotations.AddRangeAsync(ann1, ann2);
        await _context.SaveChangesAsync();

        ann1.UpdateAnnotation("[15,15,55,55]");
        ann2.UpdateAnnotation("[25,25,65,65]");

        // Act
        var count = await _repository.UpdateRangeAsync(new[] { ann1, ann2 });

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateRangeAsync_EmptyList_ReturnsZero()
    {
        // Act
        var count = await _repository.UpdateRangeAsync(Array.Empty<Annotation>());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task UpdateRangeAsync_NullList_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.UpdateRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteRangeAsync_ByIds_FetchesThenRemoves()
    {
        // CRITICAL: Fetches entities first, then removes
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var ann1 = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        var ann2 = Annotation.Create(image.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddRangeAsync(ann1, ann2);
        await _context.SaveChangesAsync();

        var ids = new[] { ann1.Id, ann2.Id };

        // Act
        var count = await _repository.DeleteRangeAsync(ids);
        await _context.SaveChangesAsync();

        // Assert
        count.Should().Be(2);
        var remaining = await _repository.GetByProjectIdAsync(project.Id);
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRangeAsync_EmptyList_ReturnsZero()
    {
        // Act
        var count = await _repository.DeleteRangeAsync(Array.Empty<int>());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteRangeAsync_NullList_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.DeleteRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteByImageIdsAsync_CascadeHelper_RemovesAll()
    {
        // CRITICAL: Cascade delete helper
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test", 1, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var ann1 = Annotation.Create(image1.Id, project.Id, projectClass.Id, null);
        var ann2 = Annotation.Create(image2.Id, project.Id, projectClass.Id, null);
        await _context.Annotations.AddRangeAsync(ann1, ann2);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.DeleteByImageIdsAsync(new[] { image1.Id, image2.Id });
        await _context.SaveChangesAsync();

        // Assert
        count.Should().Be(2);
        var all = await _repository.GetAllAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteByImageIdsAsync_EmptyList_ReturnsZero()
    {
        // Act
        var count = await _repository.DeleteByImageIdsAsync(Array.Empty<int>());

        // Assert
        count.Should().Be(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
