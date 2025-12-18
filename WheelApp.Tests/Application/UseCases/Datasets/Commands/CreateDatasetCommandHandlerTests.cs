using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Services;
using WheelApp.Application.UseCases.Datasets.Commands.CreateDataset;
using WheelApp.Application.UseCases.Images.Commands.UploadImages;
using WheelApp.Domain.Common;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Datasets.Commands;

/// <summary>
/// Tests for CreateDatasetCommandHandler
/// Tests dataset creation with optional image uploads in a single transaction
/// </summary>
public class CreateDatasetCommandHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly CreateDatasetCommandHandler _handler;
    private readonly IFileStorage _fileStorage;
    private readonly IImageValidationService _validationService;

    public CreateDatasetCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WheelAppDbContext(options);

        // Mock file storage and validation service
        _fileStorage = Substitute.For<IFileStorage>();
        _validationService = Substitute.For<IImageValidationService>();

        // Create real repositories
        var datasetRepository = new DatasetRepository(_context);
        var imageRepository = new ImageRepository(_context);
        var unitOfWork = new UnitOfWork(_context);
        var logger = Substitute.For<ILogger<CreateDatasetCommandHandler>>();

        _handler = new CreateDatasetCommandHandler(
            datasetRepository,
            imageRepository,
            _fileStorage,
            _validationService,
            unitOfWork,
            logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesDataset()
    {
        // Arrange
        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = "Test Description",
            CreatedBy = "user1",
            Files = new List<FileUploadInfo>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DatasetName.Should().Be("Test Dataset");
        result.Value.SuccessfulUploads.Should().Be(0);

        var dataset = await _context.Datasets.FirstOrDefaultAsync();
        dataset.Should().NotBeNull();
        dataset!.Name.Value.Should().Be("Test Dataset");
        dataset.Description.Value.Should().Be("Test Description");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var existingDataset = Dataset.Create("Test Dataset", "Existing", "user1");
        _context.Datasets.Add(existingDataset);
        await _context.SaveChangesAsync();

        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = "New Description",
            CreatedBy = "user2",
            Files = new List<FileUploadInfo>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        result.Error.Should().Contain("Test Dataset");

        var datasets = await _context.Datasets.ToListAsync();
        datasets.Should().HaveCount(1); // Only the original dataset
    }

    [Fact]
    public async Task Handle_WithImages_CreatesDatasetAndUploadsImages()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }); // JPG magic bytes
        var file = new FileUploadInfo
        {
            FileName = "test.jpg",
            Stream = stream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result<string>.Success($"datasets/1/{callInfo.ArgAt<string>(2)}")));

        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = "Test Description",
            CreatedBy = "user1",
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuccessfulUploads.Should().Be(1);
        result.Value.FailedUploads.Should().Be(0);
        result.Value.SuccessfulFiles.Should().Contain("test.jpg");

        var dataset = await _context.Datasets.FirstOrDefaultAsync();
        dataset.Should().NotBeNull();

        var images = await _context.Images.ToListAsync();
        images.Should().HaveCount(1);
        images[0].Name.Should().Be("test.jpg");
        images[0].DatasetId.Should().Be(dataset!.Id);
    }

    [Fact]
    public async Task Handle_ImageValidationFails_CreatesDatasetButSkipsImage()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00 }); // Invalid magic bytes
        var file = new FileUploadInfo
        {
            FileName = "invalid.jpg",
            Stream = stream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("File content does not match extension"));

        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = "Test Description",
            CreatedBy = "user1",
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuccessfulUploads.Should().Be(0);
        result.Value.FailedUploads.Should().Be(1);
        result.Value.FailedFiles.Should().Contain("invalid.jpg");
        result.Value.ErrorMessages.Should().Contain(msg => msg.Contains("invalid.jpg"));

        var dataset = await _context.Datasets.FirstOrDefaultAsync();
        dataset.Should().NotBeNull();

        var images = await _context.Images.ToListAsync();
        images.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FileStorageFails_CreatesDatasetButSkipsImage()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var file = new FileUploadInfo
        {
            FileName = "test.jpg",
            Stream = stream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Storage service unavailable")));

        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = "Test Description",
            CreatedBy = "user1",
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuccessfulUploads.Should().Be(0);
        result.Value.FailedUploads.Should().Be(1);
        result.Value.FailedFiles.Should().Contain("test.jpg");

        var dataset = await _context.Datasets.FirstOrDefaultAsync();
        dataset.Should().NotBeNull();

        var images = await _context.Images.ToListAsync();
        images.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleFiles_ProcessesAll()
    {
        // Arrange
        var files = new List<FileUploadInfo>
        {
            new() { FileName = "test1.jpg", Stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }), FileSize = 1024 },
            new() { FileName = "test2.png", Stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), FileSize = 2048 },
            new() { FileName = "test3.gif", Stream = new MemoryStream(new byte[] { 0x47, 0x49, 0x46, 0x38 }), FileSize = 512 }
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result<string>.Success($"datasets/1/{callInfo.ArgAt<string>(2)}")));

        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = "Test Description",
            CreatedBy = "user1",
            Files = files
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuccessfulUploads.Should().Be(3);
        result.Value.FailedUploads.Should().Be(0);
        result.Value.SuccessfulFiles.Should().HaveCount(3);

        var images = await _context.Images.ToListAsync();
        images.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ProgressCallback_ReportsProgress()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var file = new FileUploadInfo
        {
            FileName = "test.jpg",
            Stream = stream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("datasets/1/test.jpg")));

        var progressReports = new List<UploadProgressInfo>();
        var progress = new Progress<UploadProgressInfo>(p => progressReports.Add(p));

        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = "Test Description",
            CreatedBy = "user1",
            Files = new List<FileUploadInfo> { file },
            ProgressCallback = progress
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        progressReports.Should().NotBeEmpty();
        progressReports.Should().Contain(p => p.CurrentFileName == "test.jpg");
        progressReports.Should().Contain(p => p.Message == "Upload complete!");
    }

    [Fact]
    public async Task Handle_NullDescription_CreatesDatasetSuccessfully()
    {
        // Arrange
        var command = new CreateDatasetCommand
        {
            Name = "Test Dataset",
            Description = null,
            CreatedBy = "user1",
            Files = new List<FileUploadInfo>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dataset = await _context.Datasets.FirstOrDefaultAsync();
        dataset.Should().NotBeNull();
        dataset!.Description.Value.Should().BeNullOrEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
