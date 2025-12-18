using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Projects.Commands.UpdateProject;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Projects.Commands;

/// <summary>
/// Tests for UpdateProjectCommandHandler
/// Tests project updates with duplicate name validation within the same dataset
/// </summary>
public class UpdateProjectCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly UpdateProjectCommandHandler _handler;
    private readonly IMapper _mapper;

    public UpdateProjectCommandHandlerTests()
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

        _handler = new UpdateProjectCommandHandler(
            projectRepository,
            _mapper);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesProject()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Original Name", 0, "Original Description", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Updated Name");
        result.Value.Description.Should().Be("Updated Description");

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        updatedProject.Should().NotBeNull();
        updatedProject!.Name.Value.Should().Be("Updated Name");
        updatedProject.Description.Value.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateProjectCommand
        {
            Id = 999,
            Name = "New Name",
            Description = "New Description",
            ModifiedBy = "user1"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project");
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DuplicateNameInSameDataset_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Project 1", 0, "Test", dataset.Id, "user1");
        var project2 = Project.Create("Project 2", 0, "Test", dataset.Id, "user1");
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project2.Id,
            Name = "Project 1", // Trying to use project1's name
            Description = "Updated",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        result.Error.Should().Contain("Project 1");

        // Verify project2 was not updated
        var unchangedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project2.Id);
        unchangedProject!.Name.Value.Should().Be("Project 2");
    }

    [Fact]
    public async Task Handle_DuplicateNameInDifferentDataset_UpdatesSuccessfully()
    {
        // Arrange
        var dataset1 = Dataset.Create("Dataset 1", "Test", "user1");
        var dataset2 = Dataset.Create("Dataset 2", "Test", "user1");
        _context.Datasets.AddRange(dataset1, dataset2);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Same Name", 0, "Test", dataset1.Id, "user1");
        var project2 = Project.Create("Different Name", 0, "Test", dataset2.Id, "user1");
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project2.Id,
            Name = "Same Name", // Same as project1, but in different dataset
            Description = "Updated",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project2.Id);
        updatedProject!.Name.Value.Should().Be("Same Name");
    }

    [Fact]
    public async Task Handle_SameNameAsCurrentProject_UpdatesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Original Description", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Test Project", // Same name
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Updated Description");

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        updatedProject!.Description.Value.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_UpdateNameOnly_LeavesDescriptionUnchanged()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Original Name", 0, "Original Description", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Updated Name",
            Description = "Original Description", // Same description
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        updatedProject!.Name.Value.Should().Be("Updated Name");
        updatedProject.Description.Value.Should().Be("Original Description");
    }

    [Fact]
    public async Task Handle_UpdateDescriptionOnly_LeavesNameUnchanged()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Original Name", 0, "Original Description", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Original Name", // Same name
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        updatedProject!.Name.Value.Should().Be("Original Name");
        updatedProject.Description.Value.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_UpdateToNullDescription_ClearsDescription()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 0, "Original Description", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Test Project",
            Description = null,
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        updatedProject!.Description.Value.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_CaseInsensitiveDuplicateCheck_ReturnsFailure()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project1 = Project.Create("Test Project", 0, "Test", dataset.Id, "user1");
        var project2 = Project.Create("Another Project", 0, "Test", dataset.Id, "user1");
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project2.Id,
            Name = "test project", // Different case
            Description = "Updated",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_UpdateDoesNotChangeProjectType()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", 1, "Original", dataset.Id, "user1"); // Object Detection
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Updated Project",
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        updatedProject!.Type.Value.Should().Be(1); // Type unchanged
    }

    [Fact]
    public async Task Handle_MultipleUpdates_MaintainsConsistency()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Version 1", 0, "Description 1", dataset.Id, "user1");
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var command1 = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Version 2",
            Description = "Description 2",
            ModifiedBy = "user2"
        };

        var command2 = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Version 3",
            Description = "Description 3",
            ModifiedBy = "user3"
        };

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var finalProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        finalProject!.Name.Value.Should().Be("Version 3");
        finalProject.Description.Value.Should().Be("Description 3");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
