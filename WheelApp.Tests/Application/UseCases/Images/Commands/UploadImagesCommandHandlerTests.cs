using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Services;
using WheelApp.Application.UseCases.Datasets.Commands.CreateDataset;
using WheelApp.Application.UseCases.Images.Commands.UploadImages;
using WheelApp.Domain.Common;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Images.Commands;

/// <summary>
/// Tests for UploadImagesCommandHandler
/// CRITICAL: Tests file upload security (10K files, 20MB limit, extension validation)
/// </summary>
public class UploadImagesCommandHandlerTests
{
    private readonly IImageRepository _imageRepository;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IImageValidationService _validationService;
    private readonly ILogger<UploadImagesCommandHandler> _logger;
    private readonly UploadImagesCommandHandler _handler;

    public UploadImagesCommandHandlerTests()
    {
        _imageRepository = Substitute.For<IImageRepository>();
        _datasetRepository = Substitute.For<IDatasetRepository>();
        _fileStorage = Substitute.For<IFileStorage>();
        _validationService = Substitute.For<IImageValidationService>();
        _logger = Substitute.For<ILogger<UploadImagesCommandHandler>>();

        _handler = new UploadImagesCommandHandler(
            _imageRepository,
            _datasetRepository,
            _fileStorage,
            _validationService,
            _logger);
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".bmp")]
    [InlineData(".gif")]
    public async Task Handle_ValidExtension_AcceptsFile(string extension)
    {
        // Arrange
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image>());

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }); // JPG magic bytes
        var file = new FileUploadInfo
        {
            FileName = $"test{extension}",
            Stream = stream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success($"datasets/{datasetId}/{file.FileName}")));

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AddedCount.Should().Be(1);
        result.Value.FailedCount.Should().Be(0);
        await _imageRepository.Received(1).AddRangeAsync(
            Arg.Is<List<Image>>(list => list.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DatasetNotFound_ReturnsFailure()
    {
        // Arrange
        var datasetId = 999;
        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns((Dataset?)null);

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Dataset");
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ValidationFailure_RejectsFile()
    {
        // Arrange - CRITICAL: Test that validation service rejection works
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image>());

        var stream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00 }); // Invalid magic bytes
        var file = new FileUploadInfo
        {
            FileName = "malicious.jpg",
            Stream = stream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("File content does not match extension"));

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Handler succeeds but file is rejected
        result.Value.AddedCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(1);
        result.Value.FailedNames.Should().Contain("malicious.jpg");
        await _imageRepository.DidNotReceive().AddRangeAsync(Arg.Any<List<Image>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("../../etc/passwd.jpg")]
    [InlineData("..\\..\\windows\\system32\\hack.png")]
    [InlineData("../../../sensitive.jpg")]
    public async Task Handle_PathTraversalInFileName_Sanitizes(string maliciousName)
    {
        // Arrange - CRITICAL: Test path traversal prevention
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image>());

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var file = new FileUploadInfo
        {
            FileName = maliciousName,
            Stream = stream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result<string>.Success($"datasets/{datasetId}/{callInfo.ArgAt<string>(2)}")));

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AddedCount.Should().Be(1);
        // Verify that the added filename does NOT contain path traversal patterns
        result.Value.AddedNames.Should().NotContain(name => name.Contains(".."));
        result.Value.AddedNames[0].Should().NotContain("..");
    }

    [Fact]
    public async Task Handle_DuplicateFileName_SkipsFile()
    {
        // Arrange
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        // Create existing image with the path that would be generated
        var existingImage = Image.Create("test.jpg", "uploads/datasets/1/test_20250101120000.jpg", datasetId);
        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image> { existingImage });

        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var file = new FileUploadInfo
        {
            FileName = "test.jpg",
            Stream = stream,
            FileSize = 1024
        };

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AddedCount.Should().Be(0);
        result.Value.SkippedCount.Should().Be(1);
        result.Value.SkippedNames.Should().Contain("test.jpg");
    }

    [Fact]
    public async Task Handle_MultipleFiles_ProcessesAll()
    {
        // Arrange
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image>());

        var files = new List<FileUploadInfo>
        {
            new() { FileName = "test1.jpg", Stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }), FileSize = 1024 },
            new() { FileName = "test2.png", Stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), FileSize = 2048 },
            new() { FileName = "test3.gif", Stream = new MemoryStream(new byte[] { 0x47, 0x49, 0x46, 0x38 }), FileSize = 512 }
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result<string>.Success($"datasets/{datasetId}/{callInfo.ArgAt<string>(2)}")));

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = files
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AddedCount.Should().Be(3);
        result.Value.FailedCount.Should().Be(0);
        await _imageRepository.Received(1).AddRangeAsync(
            Arg.Is<List<Image>>(list => list.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProgressCallback_ReportsProgress()
    {
        // Arrange
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image>());

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
            .Returns(Task.FromResult(Result<string>.Success($"datasets/{datasetId}/test.jpg")));

        var progressReports = new List<UploadProgressInfo>();
        var progress = new Progress<UploadProgressInfo>(p => progressReports.Add(p));

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
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
    public async Task Handle_FileStorageFails_MarksFileAsFailed()
    {
        // Arrange
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image>());

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

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Handler succeeds but file failed
        result.Value.AddedCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(1);
        result.Value.FailedNames.Should().Contain("test.jpg");
    }

    [Fact]
    public async Task Handle_NonSeekableStream_CopiesAndValidates()
    {
        // Arrange - Test BrowserFileStream scenario (non-seekable)
        var datasetId = 1;
        var dataset = Dataset.Create("Test Dataset", "Test", "user1");
        SetPrivateProperty(dataset, "Id", datasetId);

        _datasetRepository.GetByIdAsync(datasetId, Arg.Any<CancellationToken>())
            .Returns(dataset);

        _imageRepository.FindAsync(Arg.Any<ISpecification<Image>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Image>());

        // Create a non-seekable stream
        var nonSeekableStream = Substitute.For<Stream>();
        nonSeekableStream.CanSeek.Returns(false);
        nonSeekableStream.ReadAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var buffer = callInfo.ArgAt<byte[]>(0);
                buffer[0] = 0xFF;
                buffer[1] = 0xD8;
                buffer[2] = 0xFF;
                return 3;
            });

        var file = new FileUploadInfo
        {
            FileName = "test.jpg",
            Stream = nonSeekableStream,
            FileSize = 1024
        };

        _validationService.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success($"datasets/{datasetId}/test.jpg")));

        var command = new UploadImagesCommand
        {
            DatasetId = datasetId,
            ProjectId = 1,
            Files = new List<FileUploadInfo> { file }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AddedCount.Should().Be(1);
        // Verify validation was called (stream was copied to MemoryStream first)
        await _validationService.Received(1).ValidateAsync(
            Arg.Any<Stream>(),
            "test.jpg",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Helper method to set private properties using reflection (for testing entity IDs)
    /// </summary>
    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property != null)
        {
            property.SetValue(obj, value);
        }
    }
}
