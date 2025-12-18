using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Events.DatasetEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for Dataset entity - HIGH PRIORITY: Aggregate root for managing image collections
/// </summary>
public class DatasetTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_ValidDataset_CreatesSuccessfully()
    {
        // Arrange
        var name = "Test Dataset";
        var description = "This is a test dataset";
        var createdBy = "user1";

        // Act
        var dataset = Dataset.Create(name, description, createdBy);

        // Assert
        dataset.Should().NotBeNull();
        dataset.Name.Value.Should().Be(name);
        dataset.Description.Value.Should().Be(description);
        dataset.ImageCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithNullDescription_CreatesSuccessfully()
    {
        // Act
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Assert
        dataset.Description.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceName_ThrowsValidationException(string invalidName)
    {
        // Act
        var act = () => Dataset.Create(invalidName, null, "user1");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Dataset name*cannot be empty*");
    }

    [Fact]
    public void Create_NameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var longName = new string('A', 51);  // Max is 50

        // Act
        var act = () => Dataset.Create(longName, null, "user1");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Dataset name*cannot exceed 50 characters*");
    }

    [Theory]
    [InlineData("dataset<name>")]
    [InlineData("dataset:name")]
    [InlineData("dataset\"name")]
    [InlineData("dataset/name")]
    [InlineData("dataset\\name")]
    [InlineData("dataset|name")]
    [InlineData("dataset?name")]
    [InlineData("dataset*name")]
    public void Create_NameWithInvalidCharacters_ThrowsValidationException(string invalidName)
    {
        // Act
        var act = () => Dataset.Create(invalidName, null, "user1");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Dataset name*contains invalid characters*");
    }

    [Fact]
    public void Create_DescriptionExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var longDescription = new string('A', 256);  // Max is 255

        // Act
        var act = () => Dataset.Create("Test", longDescription, "user1");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Description*cannot exceed 255 characters*");
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Create_RaisesDatasetCreatedEvent()
    {
        // Act
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Assert
        var domainEvents = dataset.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DatasetCreatedEvent>();
    }

    [Fact]
    public void AddImage_RaisesImageAddedToDatasetEvent()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");
        dataset.ClearDomainEvents();
        var image = Image.Create("test.jpg", "images/test.jpg", dataset.Id);

        // Act
        dataset.AddImage(image);

        // Assert
        var addedEvent = dataset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ImageAddedToDatasetEvent>().Subject;

        addedEvent.Dataset.Should().Be(dataset);
        addedEvent.Image.Should().Be(image);
    }

    [Fact]
    public void RemoveImage_RaisesImageRemovedFromDatasetEvent()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");
        var image = Image.Create("test.jpg", "images/test.jpg", dataset.Id);
        dataset.AddImage(image);
        dataset.ClearDomainEvents();

        // Note: We need to set the image ID since RemoveImage uses it
        // In real scenarios, EF Core would set this
        var imageIdProperty = typeof(Image).GetProperty("Id");
        imageIdProperty!.SetValue(image, 1);

        // Act
        dataset.RemoveImage(1);

        // Assert
        var removedEvent = dataset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ImageRemovedFromDatasetEvent>().Subject;

        removedEvent.Dataset.Should().Be(dataset);
        removedEvent.ImageId.Should().Be(1);
    }

    [Fact]
    public void UpdateName_RaisesDatasetUpdatedEvent()
    {
        // Arrange
        var dataset = Dataset.Create("Old Name", null, "user1");
        dataset.ClearDomainEvents();

        // Act
        dataset.UpdateName("New Name", "user2");

        // Assert
        var updatedEvent = dataset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DatasetUpdatedEvent>().Subject;

        updatedEvent.Changes.Should().ContainKey("Name");
        updatedEvent.Changes["Name"].Should().Be("Old Name -> New Name");
    }

    [Fact]
    public void UpdateDescription_RaisesDatasetUpdatedEvent()
    {
        // Arrange
        var dataset = Dataset.Create("Test", "Old description", "user1");
        dataset.ClearDomainEvents();

        // Act
        dataset.UpdateDescription("New description", "user2");

        // Assert
        var updatedEvent = dataset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DatasetUpdatedEvent>().Subject;

        updatedEvent.Changes.Should().ContainKey("Description");
        updatedEvent.Changes["Description"].Should().Be("New description");
    }

    #endregion

    #region AddImage Tests

    [Fact]
    public void AddImage_ValidImage_AddsSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");
        var image = Image.Create("test.jpg", "images/test.jpg", dataset.Id);

        // Act
        dataset.AddImage(image);

        // Assert
        dataset.Images.Should().ContainSingle();
        dataset.Images.First().Should().Be(image);
        dataset.ImageCount.Should().Be(1);
    }

    [Fact]
    public void AddImage_MultipleImages_AddsAllSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");
        var image1 = Image.Create("test1.jpg", "images/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "images/test2.jpg", dataset.Id);

        // Act
        dataset.AddImage(image1);
        dataset.AddImage(image2);

        // Assert
        dataset.Images.Should().HaveCount(2);
        dataset.Images.Should().Contain(image1);
        dataset.Images.Should().Contain(image2);
        dataset.ImageCount.Should().Be(2);
    }

    [Fact]
    public void AddImage_NullImage_ThrowsValidationException()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Act
        var act = () => dataset.AddImage(null!);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Image*cannot be null*");
    }

    #endregion

    #region RemoveImage Tests

    [Fact]
    public void RemoveImage_ExistingImage_RemovesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");
        var image = Image.Create("test.jpg", "images/test.jpg", dataset.Id);
        dataset.AddImage(image);

        // Set image ID
        var imageIdProperty = typeof(Image).GetProperty("Id");
        imageIdProperty!.SetValue(image, 1);

        // Act
        dataset.RemoveImage(1);

        // Assert
        dataset.Images.Should().BeEmpty();
        dataset.ImageCount.Should().Be(0);
    }

    [Fact]
    public void RemoveImage_NonExistentImage_ThrowsEntityNotFoundException()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Act
        var act = () => dataset.RemoveImage(999);

        // Assert
        act.Should().Throw<EntityNotFoundException>()
            .Which.EntityId.Should().Be(999);
    }

    #endregion

    #region UpdateName Tests

    [Fact]
    public void UpdateName_ValidName_UpdatesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Old Name", null, "user1");
        var newName = "New Name";
        var modifiedBy = "user2";

        // Act
        dataset.UpdateName(newName, modifiedBy);

        // Assert
        dataset.Name.Value.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_EmptyModifiedBy_ThrowsValidationException(string invalidModifiedBy)
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user1");

        // Act
        var act = () => dataset.UpdateName("New Name", invalidModifiedBy);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Modified by*cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_EmptyName_ThrowsValidationException(string invalidName)
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user1");

        // Act
        var act = () => dataset.UpdateName(invalidName, "user2");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Dataset name*cannot be empty*");
    }

    #endregion

    #region UpdateDescription Tests

    [Fact]
    public void UpdateDescription_ValidDescription_UpdatesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test", "Old", "user1");

        // Act
        dataset.UpdateDescription("New", "user2");

        // Assert
        dataset.Description.Value.Should().Be("New");
    }

    [Fact]
    public void UpdateDescription_NullDescription_UpdatesSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test", "Old", "user1");

        // Act
        dataset.UpdateDescription(null, "user2");

        // Assert
        dataset.Description.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateDescription_EmptyModifiedBy_ThrowsValidationException(string invalidModifiedBy)
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user1");

        // Act
        var act = () => dataset.UpdateDescription("New description", invalidModifiedBy);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Modified by*cannot be empty*");
    }

    #endregion

    #region AddProject Tests

    [Fact]
    public void AddProject_ValidProject_AddsSuccessfully()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Set dataset ID
        var datasetIdProperty = typeof(Dataset).GetProperty("Id");
        datasetIdProperty!.SetValue(dataset, 1);

        var project = Project.Create("Test Project", 0, null, 1, "user1");

        // Act
        dataset.AddProject(project);

        // Assert
        dataset.Projects.Should().ContainSingle();
        dataset.Projects.First().Should().Be(project);
        project.Dataset.Should().Be(dataset);
    }

    [Fact]
    public void AddProject_NullProject_ThrowsValidationException()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Act
        var act = () => dataset.AddProject(null!);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Project*cannot be null*");
    }

    [Fact]
    public void AddProject_DuplicateProject_ThrowsDuplicateEntityException()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Set dataset ID
        var datasetIdProperty = typeof(Dataset).GetProperty("Id");
        datasetIdProperty!.SetValue(dataset, 1);

        var project = Project.Create("Test Project", 0, null, 1, "user1");

        // Set project ID
        var projectIdProperty = typeof(Project).GetProperty("Id");
        projectIdProperty!.SetValue(project, 1);

        dataset.AddProject(project);

        // Act
        var act = () => dataset.AddProject(project);

        // Assert
        act.Should().Throw<DuplicateEntityException>()
            .WithMessage("*Project with ID 1*");
    }

    [Fact]
    public void AddProject_MaintainsBidirectionalRelationship()
    {
        // Arrange
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Set dataset ID
        var datasetIdProperty = typeof(Dataset).GetProperty("Id");
        datasetIdProperty!.SetValue(dataset, 1);

        var project = Project.Create("Test Project", 0, null, 1, "user1");

        // Act
        dataset.AddProject(project);

        // Assert
        project.Dataset.Should().Be(dataset);
        dataset.Projects.Should().Contain(project);
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void Images_InitiallyEmpty()
    {
        // Arrange & Act
        var dataset = Dataset.Create("Test", null, "user1");

        // Assert
        dataset.Images.Should().BeEmpty();
        dataset.ImageCount.Should().Be(0);
    }

    [Fact]
    public void Images_ReturnsReadOnlyCollection()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user1");

        // Act & Assert
        dataset.Images.Should().BeAssignableTo<IReadOnlyCollection<Image>>();
    }

    [Fact]
    public void Projects_InitiallyEmpty()
    {
        // Arrange & Act
        var dataset = Dataset.Create("Test", null, "user1");

        // Assert
        dataset.Projects.Should().BeEmpty();
    }

    [Fact]
    public void Projects_ReturnsReadOnlyCollection()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user1");

        // Act & Assert
        dataset.Projects.Should().BeAssignableTo<IReadOnlyCollection<Project>>();
    }

    #endregion

    #region Aggregate Root Invariants

    [Fact]
    public void Create_MaintainsAggregateRootInvariants()
    {
        // Act
        var dataset = Dataset.Create("Test", "Description", "user1");

        // Assert - Verify all collections are initialized
        dataset.Images.Should().NotBeNull();
        dataset.Projects.Should().NotBeNull();
    }

    [Fact]
    public void ImageCount_ReflectsActualImageCount()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user1");
        var image1 = Image.Create("test1.jpg", "images/test1.jpg", dataset.Id);
        var image2 = Image.Create("test2.jpg", "images/test2.jpg", dataset.Id);

        // Act
        dataset.AddImage(image1);
        dataset.AddImage(image2);

        // Assert
        dataset.ImageCount.Should().Be(2);
        dataset.ImageCount.Should().Be(dataset.Images.Count);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var dataset = Dataset.Create("Test", null, "user1");

        // Act
        dataset.ClearDomainEvents();

        // Assert
        dataset.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Name_ReturnsDatasetNameValueObject()
    {
        // Act
        var dataset = Dataset.Create("Test Dataset", null, "user1");

        // Assert
        dataset.Name.Should().BeOfType<DatasetName>();
        dataset.Name.Value.Should().Be("Test Dataset");
    }

    [Fact]
    public void Description_ReturnsDescriptionValueObject()
    {
        // Act
        var dataset = Dataset.Create("Test", "Test description", "user1");

        // Assert
        dataset.Description.Should().BeOfType<Description>();
        dataset.Description.Value.Should().Be("Test description");
    }

    #endregion
}
