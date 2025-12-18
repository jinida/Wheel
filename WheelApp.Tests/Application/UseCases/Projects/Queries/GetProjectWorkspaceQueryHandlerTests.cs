using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WheelApp.Application.Mappings;
using WheelApp.Application.UseCases.Projects.Queries.GetProjectWorkspace;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;
using WheelApp.Infrastructure.Persistence;
using WheelApp.Infrastructure.Persistence.Repositories;
using Xunit;

namespace WheelApp.Tests.Application.UseCases.Projects.Queries;

/// <summary>
/// Tests for GetProjectWorkspaceQueryHandler
/// Tests complex workspace loading with nested data (images, annotations, roles, classes)
/// </summary>
public class GetProjectWorkspaceQueryHandlerTests : IDisposable
{
    private readonly WheelAppDbContext _context;
    private readonly GetProjectWorkspaceQueryHandler _handler;
    private readonly IMapper _mapper;

    public GetProjectWorkspaceQueryHandlerTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<WheelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WheelAppDbContext(options);

        // Setup AutoMapper with real profiles
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProjectMappingProfile>();
            cfg.AddProfile<ProjectClassMappingProfile>();
            cfg.AddProfile<ImageMappingProfile>();
            cfg.AddProfile<AnnotationMappingProfile>();
            cfg.AddProfile<RoleMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Setup repositories
        var projectRepository = new ProjectRepository(_context);
        var projectClassRepository = new ProjectClassRepository(_context);
        var imageRepository = new ImageRepository(_context);
        var roleRepository = new RoleRepository(_context);
        var annotationRepository = new AnnotationRepository(_context);
        var logger = Substitute.For<ILogger<GetProjectWorkspaceQueryHandler>>();

        _handler = new GetProjectWorkspaceQueryHandler(
            projectRepository,
            projectClassRepository,
            imageRepository,
            roleRepository,
            annotationRepository,
            _mapper,
            logger);
    }

    [Fact]
    public async Task Handle_ValidProject_ReturnsCompleteWorkspace()
    {
        // Arrange - create complete project workspace
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var role = Role.Create(image.Id, project.Id, RoleType.Train.Value);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        var annotation = Annotation.Create(image.Id, project.Id, projectClass.Id, "[[10,20,30,40]]");
        await _context.Annotations.AddAsync(annotation);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var workspace = result.Value;

        workspace.ProjectId.Should().Be(project.Id);
        workspace.ProjectName.Should().Be("Test Project");
        workspace.DatasetId.Should().Be(dataset.Id);
        workspace.ProjectType.Should().Be(ProjectType.ObjectDetection.Value);

        workspace.Images.Should().HaveCount(1);
        workspace.ProjectClasses.Should().HaveCount(1);
        workspace.RoleTypes.Should().HaveCount(4); // Train, Validation, Test, None
        workspace.ProjectTypes.Should().HaveCount(4); // Classification, ObjectDetection, Segmentation, AnomalyDetection
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsFailure()
    {
        // Arrange
        var query = new GetProjectWorkspaceQuery { ProjectId = 999 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project with ID 999 not found");
    }

    [Fact]
    public async Task Handle_ProjectWithMultipleClasses_ReturnsAllClasses()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var carClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        var personClass = ProjectClass.Create(project.Id, 1, "Person", "#00FF00");
        var bikeClass = ProjectClass.Create(project.Id, 2, "Bike", "#0000FF");
        await _context.ProjectClasses.AddRangeAsync(carClass, personClass, bikeClass);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectClasses.Should().HaveCount(3);
        result.Value.ProjectClasses.Should().Contain(c => c.Name == "Car");
        result.Value.ProjectClasses.Should().Contain(c => c.Name == "Person");
        result.Value.ProjectClasses.Should().Contain(c => c.Name == "Bike");
    }

    [Fact]
    public async Task Handle_ProjectWithMultipleImages_ReturnsAllImages()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var image1 = Image.Create("test1.jpg", "path/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "path/test2.jpg", dataset.Id);
        var image3 = Image.Create("test3.jpg", "path/test3.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(image1, image2, image3);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Images.Should().HaveCount(3);
        result.Value.Images.Should().Contain(i => i.Name == "test1.jpg");
        result.Value.Images.Should().Contain(i => i.Name == "test2.jpg");
        result.Value.Images.Should().Contain(i => i.Name == "test3.jpg");
    }

    [Fact]
    public async Task Handle_ImagesWithRoles_MapsRolesCorrectly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var trainImage = Image.Create("train.jpg", "path/train.jpg", dataset.Id);
        var validImage = Image.Create("valid.jpg", "path/valid.jpg", dataset.Id);
        var testImage = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddRangeAsync(trainImage, validImage, testImage);
        await _context.SaveChangesAsync();

        var trainRole = Role.Create(trainImage.Id, project.Id, RoleType.Train.Value);
        var validRole = Role.Create(validImage.Id, project.Id, RoleType.Validation.Value);
        var testRole = Role.Create(testImage.Id, project.Id, RoleType.Test.Value);
        await _context.Roles.AddRangeAsync(trainRole, validRole, testRole);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var workspace = result.Value;

        var trainImageDto = workspace.Images.First(i => i.Name == "train.jpg");
        trainImageDto.RoleType.Should().NotBeNull();
        trainImageDto.RoleType!.Value.Should().Be(RoleType.Train.Value);

        var validImageDto = workspace.Images.First(i => i.Name == "valid.jpg");
        validImageDto.RoleType.Should().NotBeNull();
        validImageDto.RoleType!.Value.Should().Be(RoleType.Validation.Value);

        var testImageDto = workspace.Images.First(i => i.Name == "test.jpg");
        testImageDto.RoleType.Should().NotBeNull();
        testImageDto.RoleType!.Value.Should().Be(RoleType.Test.Value);
    }

    [Fact]
    public async Task Handle_ImagesWithAnnotations_MapsAnnotationsCorrectly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var projectClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        await _context.ProjectClasses.AddAsync(projectClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var annotation1 = Annotation.Create(image.Id, project.Id, projectClass.Id, "[[10,20,30,40]]");
        var annotation2 = Annotation.Create(image.Id, project.Id, projectClass.Id, "[[50,60,70,80]]");
        await _context.Annotations.AddRangeAsync(annotation1, annotation2);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var imageDto = result.Value.Images[0];

        imageDto.Annotation.Should().HaveCount(2);
        imageDto.Annotation.Should().OnlyContain(a => a.imageId == image.Id);
        imageDto.Annotation.Should().OnlyContain(a => a.classDto.Name == "Car");
    }

    [Fact]
    public async Task Handle_AnnotationsWithClassInfo_MapsClassDataCorrectly()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var carClass = ProjectClass.Create(project.Id, 0, "Car", "#FF0000");
        var personClass = ProjectClass.Create(project.Id, 1, "Person", "#00FF00");
        await _context.ProjectClasses.AddRangeAsync(carClass, personClass);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var carAnnotation = Annotation.Create(image.Id, project.Id, carClass.Id, "[[10,20,30,40]]");
        var personAnnotation = Annotation.Create(image.Id, project.Id, personClass.Id, "[[50,60,70,80]]");
        await _context.Annotations.AddRangeAsync(carAnnotation, personAnnotation);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var imageDto = result.Value.Images[0];

        var carAnnotationDto = imageDto.Annotation.First(a => a.classDto.Name == "Car");
        carAnnotationDto.classDto.ClassIdx.Should().Be(0);
        carAnnotationDto.classDto.Color.Should().Be("#FF0000");

        var personAnnotationDto = imageDto.Annotation.First(a => a.classDto.Name == "Person");
        personAnnotationDto.classDto.ClassIdx.Should().Be(1);
        personAnnotationDto.classDto.Color.Should().Be("#00FF00");
    }

    [Fact]
    public async Task Handle_ImageWithoutRole_RoleTypeIsNull()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var imageDto = result.Value.Images[0];
        imageDto.RoleType.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ImageWithoutAnnotations_AnnotationListIsEmpty()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.ObjectDetection.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var image = Image.Create("test.jpg", "path/test.jpg", dataset.Id);
        await _context.Images.AddAsync(image);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var imageDto = result.Value.Images[0];
        imageDto.Annotation.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAllRoleTypes()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RoleTypes.Should().HaveCount(4);
        result.Value.RoleTypes.Should().Contain(r => r.Name == "Train");
        result.Value.RoleTypes.Should().Contain(r => r.Name == "Validation");
        result.Value.RoleTypes.Should().Contain(r => r.Name == "Test");
        result.Value.RoleTypes.Should().Contain(r => r.Name == "None");
    }

    [Fact]
    public async Task Handle_ReturnsAllProjectTypes()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Test Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectTypes.Should().HaveCount(4);
        result.Value.ProjectTypes.Should().Contain(p => p.Name == "Classification");
        result.Value.ProjectTypes.Should().Contain(p => p.Name == "Object Detection");
        result.Value.ProjectTypes.Should().Contain(p => p.Name == "Segmentation");
        result.Value.ProjectTypes.Should().Contain(p => p.Name == "Anomaly Detection");
    }

    [Fact]
    public async Task Handle_DifferentProjectTypes_MapsTypeCorrectly()
    {
        // Arrange - test Classification project
        var dataset = Dataset.Create("Test Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Classification Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectType.Should().Be(ProjectType.Classification.Value);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyCollections()
    {
        // Arrange - project with no classes, images, roles, or annotations
        var dataset = Dataset.Create("Empty Dataset", null, "test-user");
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        var project = Project.Create("Empty Project", ProjectType.Classification.Value, null, dataset.Id, "test-user");
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var query = new GetProjectWorkspaceQuery { ProjectId = project.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Images.Should().BeEmpty();
        result.Value.ProjectClasses.Should().BeEmpty();
        result.Value.RoleTypes.Should().HaveCount(4); // Static list always returned
        result.Value.ProjectTypes.Should().HaveCount(4); // Static list always returned
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
