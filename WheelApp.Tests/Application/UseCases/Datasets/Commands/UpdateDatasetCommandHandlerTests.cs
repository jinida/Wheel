using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Application.Common.Exceptions;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Datasets.Commands.UpdateDataset;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using WheelApp.Tests.Common;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Datasets.Commands;

/// <summary>
/// Tests for UpdateDatasetCommandHandler
/// Tests dataset updates with duplicate name validation
/// </summary>
public class UpdateDatasetCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly UpdateDatasetCommandHandler _handler;
    private readonly IMapper _mapper;
    private readonly IDatasetRepository _repository;

    public UpdateDatasetCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WheelAppDbContext(options);

        // Setup AutoMapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DatasetMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Use real repository - we'll manually save in tests
        _repository = new DatasetRepository(_context);

        _handler = new UpdateDatasetCommandHandler(
            _repository,
            _mapper);
    }

    /// <summary>
    /// Helper to execute command (TransactionBehavior in production handles SaveChangesAsync)
    /// </summary>
    private async Task<Result<DatasetDto>> ExecuteCommandAsync(UpdateDatasetCommand command)
    {
        return await _handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesDataset()
    {
        // Arrange
        var dataset = Dataset.Create("Original Name", "Original Description", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new UpdateDatasetCommand
        {
            Id = dataset.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Updated Name");
        result.Value.Description.Should().Be("Updated Description");

        var updatedDataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == dataset.Id);
        updatedDataset.Should().NotBeNull();
        updatedDataset!.Name.Value.Should().Be("Updated Name");
        updatedDataset.Description.Value.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_DatasetNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new UpdateDatasetCommand
        {
            Id = 999,
            Name = "New Name",
            Description = "New Description",
            ModifiedBy = "user1"
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Dataset*999*");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var dataset1 = Dataset.Create("Existing Dataset", "Description 1", "user1");
        var dataset2 = Dataset.Create("Test Dataset", "Description 2", "user1");
        _context.Datasets.AddRange(dataset1, dataset2);
        await _context.SaveChangesAsync();

        var command = new UpdateDatasetCommand
        {
            Id = dataset2.Id,
            Name = "Existing Dataset",  // Try to use existing name
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        // No save needed - operation should fail

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        result.Error.Should().Contain("Existing Dataset");
    }

    [Fact]
    public async Task Handle_SameNameAsCurrentDataset_UpdatesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Original Description", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new UpdateDatasetCommand
        {
            Id = dataset.Id,
            Name = "Test Dataset",  // Same name
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_UpdateNameOnly_LeavesDescriptionUnchanged()
    {
        // Arrange
        var dataset = Dataset.Create("Original Name", "Original Description", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new UpdateDatasetCommand
        {
            Id = dataset.Id,
            Name = "Updated Name",
            Description = "Original Description",  // Keep same
            ModifiedBy = "user2"
        };

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedDataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == dataset.Id);
        updatedDataset!.Name.Value.Should().Be("Updated Name");
        updatedDataset.Description.Value.Should().Be("Original Description");
    }

    [Fact]
    public async Task Handle_UpdateDescriptionOnly_LeavesNameUnchanged()
    {
        // Arrange
        var dataset = Dataset.Create("Original Name", "Original Description", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new UpdateDatasetCommand
        {
            Id = dataset.Id,
            Name = "Original Name",  // Keep same
            Description = "Updated Description",
            ModifiedBy = "user2"
        };

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedDataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == dataset.Id);
        updatedDataset!.Name.Value.Should().Be("Original Name");
        updatedDataset.Description.Value.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_UpdateToNullDescription_ClearsDescription()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", "Original Description", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command = new UpdateDatasetCommand
        {
            Id = dataset.Id,
            Name = "Test Dataset",
            Description = null,
            ModifiedBy = "user2"
        };

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedDataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == dataset.Id);
        updatedDataset!.Description.Value.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_MultipleUpdates_MaintainsConsistency()
    {
        // Arrange
        var dataset = Dataset.Create("Version 1", "Description 1", "user1");
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        var command1 = new UpdateDatasetCommand
        {
            Id = dataset.Id,
            Name = "Version 2",
            Description = "Description 2",
            ModifiedBy = "user2"
        };

        var command2 = new UpdateDatasetCommand
        {
            Id = dataset.Id,
            Name = "Version 3",
            Description = "Description 3",
            ModifiedBy = "user3"
        };

        // Act
        var result1 = await ExecuteAndSaveAsync(command1);
        var result2 = await ExecuteAndSaveAsync(command2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var finalDataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == dataset.Id);
        finalDataset!.Name.Value.Should().Be("Version 3");
        finalDataset.Description.Value.Should().Be("Description 3");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
