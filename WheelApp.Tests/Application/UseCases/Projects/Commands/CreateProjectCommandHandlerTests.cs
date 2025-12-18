using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Projects.Commands.CreateProject;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Projects.Commands;

/// <summary>
/// Tests for CreateProjectCommandHandler
/// Tests project creation with automatic Role entries for all dataset images
/// </summary>
public class CreateProjectCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly CreateProjectCommandHandler _handler;
    private readonly IMapper _mapper;

    public CreateProjectCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WheelAppDbContext(options);

        // Setup AutoMapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProjectMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Create real repositories
        var projectRepository = new ProjectRepository(_context);
        var datasetRepository = new DatasetRepository(_context);
        var imageRepository = new ImageRepository(_context);
        var projectClassRepository = new ProjectClassRepository(_context);
        var roleRepository = new RoleRepository(_context);
        var unitOfWork = new UnitOfWork(_context);
        var logger = Substitute.For<ILogger<CreateProjectCommandHandler>>();

        _handler = new CreateProjectCommandHandler(
            projectRepository,
            datasetRepository,
            imageRepository,
            projectClassRepository,
            roleRepository,
            unitOfWork,
            _mapper,
            logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProject()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = dataset.Id,
            Type = 0, // Classification
            Description = "Test Description",
            CreatedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Test Project");
        result.Value.DatasetId.Should().Be(dataset.Id);

        var project = await _context.Projects.FirstOrDefaultAsync();
        project.Should().NotBeNull();
        project!.Name.Value.Should().Be("Test Project");
        project.Type.Value.Should().Be(0);
        project.Description.Value.Should().Be("Test Description");
    }

    [Fact]
    public async Task Handle_DatasetNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = 999,
            Type = 0,
            Description = "Test",
            CreatedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Dataset");
        result.Error.Should().Contain("not found");

        var projects = await _context.Projects.ToListAsync();
        projects.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DuplicateNameInSameDataset_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var existingProject = Project.Create("Test Project", 0, "Existing", dataset.Id, "user1");
        _context.Projects.Add(existingProject);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = dataset.Id,
            Type = 0,
            Description = "New",
            CreatedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        result.Error.Should().Contain("Test Project");

        var projects = await _context.Projects.ToListAsync();
        projects.Should().HaveCount(1); // Only the original project
    }

    [Fact]
    public async Task Handle_DuplicateNameInDifferentDataset_CreatesSuccessfully()
    {
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", "Test", "user1");
        var dataset2 = Dataset.Create("Dataset 2", "Test", "user1");
        _context.Datasets.AddRange(dataset1, dataset2);
        await _context.SaveChangesAsync();

        var existingProject = Project.Create("Test Project", 0, "Existing", dataset1.Id, "user1");
        _context.Projects.Add(existingProject);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = dataset2.Id,
            Type = 0,
            Description = "New",
            CreatedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var projects = await _context.Projects.ToListAsync();
        projects.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithImages_CreatesRoleEntriesForAllImages()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("img1.jpg", "uploads/datasets/1/img1.jpg", dataset.Id);
        var image2 = Image.Create("img2.jpg", "uploads/datasets/1/img2.jpg", dataset.Id);
        var image3 = Image.Create("img3.jpg", "uploads/datasets/1/img3.jpg", dataset.Id);
        _context.Images.AddRange(image1, image2, image3);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = dataset.Id,
            Type = 0,
            Description = "Test",
            CreatedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var project = await _context.Projects.FirstOrDefaultAsync();
        project.Should().NotBeNull();

        var roles = await _context.Roles.Where(r => r.ProjectId == project!.Id).ToListAsync();
        roles.Should().HaveCount(3);
        roles.Should().OnlyContain(r => r.RoleType.Value == RoleType.None.Value);
        roles.Select(r => r.ImageId).Should().Contain(new[] { image1.Id, image2.Id, image3.Id });
    }

    [Fact]
    public async Task Handle_NoImages_CreatesProjectWithoutRoles()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = dataset.Id,
            Type = 0,
            Description = "Test",
            CreatedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var project = await _context.Projects.FirstOrDefaultAsync();
        project.Should().NotBeNull();

        var roles = await _context.Roles.ToListAsync();
        roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AnomalyDetectionType_CreatesDefaultClasses()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Anomaly Project",
            DatasetId = dataset.Id,
            Type = 3, // Anomaly Detection
            Description = "Test",
            CreatedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var project = await _context.Projects.FirstOrDefaultAsync();
        project.Should().NotBeNull();

        var classes = await _context.ProjectClasses.Where(c => c.ProjectId == project!.Id).ToListAsync();
        classes.Should().HaveCount(2);

        var normalClass = classes.FirstOrDefault(c => c.ClassIdx == 0);
        normalClass.Should().NotBeNull();
        normalClass!.Name.Should().Be("Normal");
        normalClass.Color.Should().Be("#FF0000");

        var anomalyClass = classes.FirstOrDefault(c => c.ClassIdx == 1);
        anomalyClass.Should().NotBeNull();
        anomalyClass!.Name.Should().Be("Anomaly");
        anomalyClass.Color.Should().Be("#0000FF");
    }

    [Theory]
    [InlineData(0)] // Classification
    [InlineData(1)] // Object Detection
    [InlineData(2)] // Segmentation
    public async Task Handle_NonAnomalyDetectionType_DoesNotCreateDefaultClasses(int projectType)
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = dataset.Id,
            Type = projectType,
            Description = "Test",
            CreatedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var project = await _context.Projects.FirstOrDefaultAsync();
        project.Should().NotBeNull();

        var classes = await _context.ProjectClasses.ToListAsync();
        classes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullDescription_CreatesProjectSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            DatasetId = dataset.Id,
            Type = 0,
            Description = null,
            CreatedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var project = await _context.Projects.FirstOrDefaultAsync();
        project.Should().NotBeNull();
        project!.Description.Value.Should().BeNullOrEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
