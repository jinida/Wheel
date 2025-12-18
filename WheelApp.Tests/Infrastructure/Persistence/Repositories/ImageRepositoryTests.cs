using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Repositories;

public class ImageRepositoryTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly ImageRepository _repository;

    public ImageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);
        _repository = new ImageRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAnnotations()
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
        var result = await _repository.GetByIdAsync(image.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Annotations.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByDatasetIdAsync_IncludesAnnotations()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByDatasetIdAsync(dataset.Id);

        // Assert
        results.Should().HaveCount(1);
        results.First().Annotations.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByDatasetIdAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetByDatasetIdAsync(dataset.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    [Fact]
    public async Task GetByIdsAsync_ValidIds_ReturnsImages()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByIdsAsync(new[] { image1.Id, image2.Id });

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
    public async Task GetExistingIdsAsync_ReturnsOnlyIds()
    {
        // CRITICAL: Returns List<int>, not entities
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        // Act
        var existingIds = await _repository.GetExistingIdsAsync(new[] { image1.Id, image2.Id, 999 });

        // Assert
        existingIds.Should().HaveCount(2);
        existingIds.Should().Contain(image1.Id);
        existingIds.Should().Contain(image2.Id);
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
        // CRITICAL: Counts ALL datasets
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", null, "user");
        var dataset2 = Dataset.Create("Dataset 2", null, "user");
        await _context.Datasets.AddRangeAsync(dataset1, dataset2);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            await _context.Images.AddAsync(Image.Create($"img{i}.jpg", $"/ds1/img{i}.jpg", dataset1.Id));
        }
        for (int i = 0; i < 3; i++)
        {
            await _context.Images.AddAsync(Image.Create($"img{i}.jpg", $"/ds2/img{i}.jpg", dataset2.Id));
        }
        await _context.SaveChangesAsync();

        // Act
        var counts = await _repository.GetCountsByDatasetAsync();

        // Assert
        counts.Should().ContainKey(dataset1.Id);
        counts.Should().ContainKey(dataset2.Id);
        counts[dataset1.Id].Should().Be(5);
        counts[dataset2.Id].Should().Be(3);
    }

    [Fact]
    public async Task GetCountsByDatasetIdsAsync_OptimizedForSpecificDatasets()
    {
        // CRITICAL: More efficient than GetCountsByDatasetAsync when filtering
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", null, "user");
        var dataset2 = Dataset.Create("Dataset 2", null, "user");
        var dataset3 = Dataset.Create("Dataset 3", null, "user");
        await _context.Datasets.AddRangeAsync(dataset1, dataset2, dataset3);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            await _context.Images.AddAsync(Image.Create($"img{i}.jpg", $"/ds1/img{i}.jpg", dataset1.Id));
        }
        for (int i = 0; i < 3; i++)
        {
            await _context.Images.AddAsync(Image.Create($"img{i}.jpg", $"/ds2/img{i}.jpg", dataset2.Id));
        }
        await _context.SaveChangesAsync();

        // Act - Only query datasets 1 and 2
        var counts = await _repository.GetCountsByDatasetIdsAsync(new List<int> { dataset1.Id, dataset2.Id });

        // Assert
        counts.Should().ContainKey(dataset1.Id);
        counts.Should().ContainKey(dataset2.Id);
        counts.Should().NotContainKey(dataset3.Id); // Not queried
        counts[dataset1.Id].Should().Be(5);
        counts[dataset2.Id].Should().Be(3);
    }

    [Fact]
    public async Task GetWithAnnotationsAsync_FiltersAnnotationsByProjectId()
    {
        // CRITICAL: FILTERED Include - only returns annotations for specified projectId
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 1, null, dataset.Id, "user");
        var project2 = Project.Create("Project 2", 1, null, dataset.Id, "user");
        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project1.Id, 0, "Person", "#FF0000");
        var class2 = ProjectClass.Create(project2.Id, 0, "Car", "#00FF00");
        await _context.ProjectClasses.AddRangeAsync(class1, class2);
        await _context.SaveChangesAsync();

        var ann1 = Annotation.Create(image.Id, project1.Id, class1.Id, "[10,10,50,50]");
        var ann2 = Annotation.Create(image.Id, project2.Id, class2.Id, "[20,20,60,60]");
        await _context.Annotations.AddRangeAsync(ann1, ann2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetWithAnnotationsAsync(dataset.Id, project1.Id);

        // Assert
        results.Should().HaveCount(1);
        var imageResult = results.First();
        imageResult.Annotations.Should().HaveCount(1); // Only project1 annotations
        imageResult.Annotations.First().ProjectId.Should().Be(project1.Id);
    }

    [Fact]
    public async Task GetWithAnnotationsAsync_MultipleImagesWithFilteredAnnotations()
    {
        // Verify filtered includes work across multiple images
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "/test2.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 1, null, dataset.Id, "user");
        var project2 = Project.Create("Project 2", 1, null, dataset.Id, "user");
        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        var class1 = ProjectClass.Create(project1.Id, 0, "Person", "#FF0000");
        var class2 = ProjectClass.Create(project2.Id, 0, "Car", "#00FF00");
        await _context.ProjectClasses.AddRangeAsync(class1, class2);
        await _context.SaveChangesAsync();

        // Image1 has annotations for both projects
        var ann1 = Annotation.Create(image1.Id, project1.Id, class1.Id, "[10,10,50,50]");
        var ann2 = Annotation.Create(image1.Id, project2.Id, class2.Id, "[20,20,60,60]");
        // Image2 has annotation only for project1
        var ann3 = Annotation.Create(image2.Id, project1.Id, class1.Id, "[30,30,70,70]");
        await _context.Annotations.AddRangeAsync(ann1, ann2, ann3);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetWithAnnotationsAsync(dataset.Id, project1.Id);

        // Assert
        results.Should().HaveCount(2);
        // Image1 should only have project1 annotation (not project2)
        var img1Result = results.First(i => i.Id == image1.Id);
        img1Result.Annotations.Should().HaveCount(1);
        img1Result.Annotations.First().ProjectId.Should().Be(project1.Id);
        // Image2 should have its project1 annotation
        var img2Result = results.First(i => i.Id == image2.Id);
        img2Result.Annotations.Should().HaveCount(1);
        img2Result.Annotations.First().ProjectId.Should().Be(project1.Id);
    }

    [Fact]
    public async Task GetWithAnnotationsAsync_ReturnsAsNoTracking()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var project = Project.Create("Project", 0, null, dataset.Id, "user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetWithAnnotationsAsync(dataset.Id, project.Id);

        // Assert
        results.Should().HaveCount(1);
        _context.ChangeTracker.Entries().Should().BeEmpty(); // No tracking
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
