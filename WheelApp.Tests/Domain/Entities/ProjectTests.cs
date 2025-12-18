using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Events.ProjectEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for Project entity - CRITICAL: Aggregate root with business rules
/// </summary>
public class ProjectTests
{
    #region Factory Method Tests

    [Theory]
    [InlineData(0, "Classification")]
    [InlineData(1, "ObjectDetection")]
    [InlineData(2, "Segmentation")]
    [InlineData(3, "AnomalyDetection")]
    public void Create_ValidProjectType_CreatesSuccessfully(int typeValue, string expectedTypeName)
    {
        // Arrange
        string name = "Test Project";
        int datasetId = 1;
        string createdBy = "user1";

        // Act
        var project = Project.Create(name, typeValue, null, datasetId, createdBy);

        // Assert
        project.Should().NotBeNull();
        project.Name.Value.Should().Be(name);
        project.Type.Value.Should().Be(typeValue);
        project.Type.Name.Should().Be(expectedTypeName);
        project.DatasetId.Should().Be(datasetId);
        project.Description.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithDescription_CreatesSuccessfully()
    {
        // Arrange
        string name = "Test Project";
        string description = "This is a test project";

        // Act
        var project = Project.Create(name, 0, description, 1, "user1");

        // Assert
        project.Description.Value.Should().Be(description);
    }

    [Fact]
    public void Create_WithNullDescription_CreatesSuccessfully()
    {
        // Act
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Assert
        project.Description.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceName_ThrowsValidationException(string invalidName)
    {
        // Act
        var act = () => Project.Create(invalidName, 0, null, 1, "user1");

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Create_NameExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var longName = new string('A', 51);  // Max is 50

        // Act
        var act = () => Project.Create(longName, 0, null, 1, "user1");

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public void Create_InvalidProjectType_ThrowsException(int invalidType)
    {
        // Act
        var act = () => Project.Create("Test", invalidType, null, 1, "user1");

        // Assert
        act.Should().Throw<Exception>();  // InvalidTrainingStatusException or similar
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Create_RaisesProjectCreatedEvent()
    {
        // Act
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Assert
        var domainEvents = project.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProjectCreatedEvent>();
    }

    [Fact]
    public void ChangeType_RaisesProjectTypeChangedEvent()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        project.ClearDomainEvents();

        // Act
        project.ChangeType(1, "user2");

        // Assert
        var domainEvents = project.DomainEvents;
        var typeChangedEvent = domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProjectTypeChangedEvent>().Subject;

        typeChangedEvent.OldType.Value.Should().Be(0);
        typeChangedEvent.NewType.Value.Should().Be(1);
    }

    [Fact]
    public void UpdateName_RaisesProjectUpdatedEvent()
    {
        // Arrange
        var project = Project.Create("Old Name", 0, null, 1, "user1");
        project.ClearDomainEvents();

        // Act
        project.UpdateName("New Name", "user2");

        // Assert
        var domainEvents = project.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProjectUpdatedEvent>();
    }

    [Fact]
    public void UpdateDescription_RaisesProjectUpdatedEvent()
    {
        // Arrange
        var project = Project.Create("Test", 0, "Old description", 1, "user1");
        project.ClearDomainEvents();

        // Act
        project.UpdateDescription("New description", "user2");

        // Assert
        var domainEvents = project.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProjectUpdatedEvent>();
    }

    #endregion

    #region ChangeType Tests

    [Fact]
    public void ChangeType_ValidType_ChangesSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        project.ClearDomainEvents();

        // Act
        project.ChangeType(1, "user2");

        // Assert
        project.Type.Value.Should().Be(1);
        project.Type.Name.Should().Be("ObjectDetection");
        project.ModifiedBy.Should().Be("user2");
    }

    [Fact]
    public void ChangeType_SameType_DoesNotRaiseEvent()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        project.ClearDomainEvents();

        // Act
        project.ChangeType(0, "user2");  // Same type

        // Assert
        project.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ChangeType_EmptyModifiedBy_ThrowsValidationException(string invalidModifiedBy)
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        var act = () => project.ChangeType(1, invalidModifiedBy);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Modified by*cannot be empty*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public void ChangeType_InvalidType_ThrowsException(int invalidType)
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        var act = () => project.ChangeType(invalidType, "user2");

        // Assert
        act.Should().Throw<Exception>();
    }

    #endregion

    #region Class Management Tests

    [Fact]
    public void AddClass_ValidClass_AddsSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");

        // Act
        project.AddClass(projectClass);

        // Assert
        project.Classes.Should().ContainSingle();
        project.Classes.First().Should().Be(projectClass);
    }

    [Fact]
    public void AddClass_MultipleClasses_AddsAllSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var class1 = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "Car", "#00FF00");

        // Act
        project.AddClass(class1);
        project.AddClass(class2);

        // Assert
        project.Classes.Should().HaveCount(2);
        project.Classes.Should().Contain(class1);
        project.Classes.Should().Contain(class2);
    }

    [Fact]
    public void AddClass_NullClass_ThrowsValidationException()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        var act = () => project.AddClass(null!);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Project class*cannot be null*");
    }

    [Fact]
    public void AddClass_DuplicateClassIndex_ThrowsDuplicateEntityException()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var class1 = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 0, "Car", "#00FF00");  // Same index
        project.AddClass(class1);

        // Act
        var act = () => project.AddClass(class2);

        // Assert
        act.Should().Throw<DuplicateEntityException>()
            .WithMessage("*Class with index 0*");
    }

    [Fact]
    public void RemoveClass_ExistingClass_RemovesSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        project.AddClass(projectClass);

        // Act
        project.RemoveClass(projectClass.Id);

        // Assert
        project.Classes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveClass_NonExistentClass_ThrowsEntityNotFoundException()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        var act = () => project.RemoveClass(999);

        // Assert
        act.Should().Throw<EntityNotFoundException>()
            .Which.EntityId.Should().Be(999);
    }

    [Fact]
    public void Classes_ReturnsReadOnlyCollection()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act & Assert
        project.Classes.Should().BeAssignableTo<IReadOnlyCollection<ProjectClass>>();
    }

    #endregion

    #region UpdateName Tests

    [Fact]
    public void UpdateName_ValidName_UpdatesSuccessfully()
    {
        // Arrange
        var project = Project.Create("Old Name", 0, null, 1, "user1");
        string newName = "New Name";
        string modifiedBy = "user2";

        // Act
        project.UpdateName(newName, modifiedBy);

        // Assert
        project.Name.Value.Should().Be(newName);
        project.ModifiedBy.Should().Be(modifiedBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_EmptyModifiedBy_ThrowsValidationException(string invalidModifiedBy)
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        var act = () => project.UpdateName("New Name", invalidModifiedBy);

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
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        var act = () => project.UpdateName(invalidName, "user2");

        // Assert
        act.Should().Throw<ValidationException>();
    }

    #endregion

    #region UpdateDescription Tests

    [Fact]
    public void UpdateDescription_ValidDescription_UpdatesSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, "Old", 1, "user1");

        // Act
        project.UpdateDescription("New", "user2");

        // Assert
        project.Description.Value.Should().Be("New");
        project.ModifiedBy.Should().Be("user2");
    }

    [Fact]
    public void UpdateDescription_NullDescription_UpdatesSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, "Old", 1, "user1");

        // Act
        project.UpdateDescription(null, "user2");

        // Assert
        project.Description.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateDescription_EmptyModifiedBy_ThrowsValidationException(string invalidModifiedBy)
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        var act = () => project.UpdateDescription("New description", invalidModifiedBy);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Modified by*cannot be empty*");
    }

    #endregion

    #region Dataset Relationship Tests

    [Fact]
    public void SetDataset_ValidDataset_SetsSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var dataset = Dataset.Create("Dataset", null, "user1");

        // Act
        dataset.AddProject(project);  // SetDataset is internal, called via Dataset.AddProject

        // Assert
        project.Dataset.Should().NotBeNull();
        project.Dataset.Should().Be(dataset);
    }

    [Fact]
    public void Dataset_InitiallyNull()
    {
        // Arrange & Act
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Assert
        project.Dataset.Should().BeNull();
    }

    #endregion

    #region Training Collection Tests

    [Fact]
    public void Trainings_InitiallyEmpty()
    {
        // Arrange & Act
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Assert
        project.Trainings.Should().BeEmpty();
    }

    [Fact]
    public void Trainings_ReturnsReadOnlyCollection()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act & Assert
        project.Trainings.Should().BeAssignableTo<IReadOnlyCollection<Training>>();
    }

    #endregion

    #region Annotation Collection Tests

    [Fact]
    public void Annotations_InitiallyEmpty()
    {
        // Arrange & Act
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Assert
        project.Annotations.Should().BeEmpty();
    }

    [Fact]
    public void Annotations_ReturnsReadOnlyCollection()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act & Assert
        project.Annotations.Should().BeAssignableTo<IReadOnlyCollection<Annotation>>();
    }

    #endregion

    #region Business Rule Tests - StartTraining (Deprecated)

    [Fact]
    public void StartTraining_WithClasses_CreatesTrainingSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        project.AddClass(projectClass);

#pragma warning disable CS0618 // Type or member is obsolete
        // Act
        var training = project.StartTraining("Test Training");
#pragma warning restore CS0618

        // Assert
        training.Should().NotBeNull();
        training.ProjectId.Should().Be(project.Id);
        training.Name.Value.Should().Be("Test Training");
        project.Trainings.Should().ContainSingle();
    }

    [Fact]
    public void StartTraining_WithoutClasses_ThrowsDomainOperationException()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

#pragma warning disable CS0618 // Type or member is obsolete
        // Act
        var act = () => project.StartTraining("Test Training");
#pragma warning restore CS0618

        // Assert
        act.Should().Throw<DomainOperationException>()
            .WithMessage("*Cannot start training without project classes*");
    }

    [Fact]
    public void StartTraining_WithActiveTraining_ThrowsDomainOperationException()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        project.AddClass(projectClass);

#pragma warning disable CS0618 // Type or member is obsolete
        project.StartTraining("Training 1");  // First training (Pending)

        // Act
        var act = () => project.StartTraining("Training 2");  // Second training while first is active
#pragma warning restore CS0618

        // Assert
        act.Should().Throw<DomainOperationException>()
            .WithMessage("*Cannot start training while another training is active*");
    }

    [Fact]
    public void StartTraining_AfterCompletedTraining_CreatesNewTrainingSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        project.AddClass(projectClass);

#pragma warning disable CS0618 // Type or member is obsolete
        var training1 = project.StartTraining("Training 1");
#pragma warning restore CS0618

        training1.UpdateStatus(1);  // Running
        training1.Complete();  // Completed

#pragma warning disable CS0618 // Type or member is obsolete
        // Act
        var training2 = project.StartTraining("Training 2");
#pragma warning restore CS0618

        // Assert
        training2.Should().NotBeNull();
        project.Trainings.Should().HaveCount(2);
    }

    [Fact]
    public void StartTraining_AfterFailedTraining_CreatesNewTrainingSuccessfully()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var projectClass = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        project.AddClass(projectClass);

#pragma warning disable CS0618 // Type or member is obsolete
        var training1 = project.StartTraining("Training 1");
#pragma warning restore CS0618

        training1.UpdateStatus(1);  // Running
        training1.Fail("Error occurred");  // Failed

#pragma warning disable CS0618 // Type or member is obsolete
        // Act
        var training2 = project.StartTraining("Training 2");
#pragma warning restore CS0618

        // Assert
        training2.Should().NotBeNull();
        project.Trainings.Should().HaveCount(2);
    }

    #endregion

    #region Aggregate Root Invariants

    [Fact]
    public void Create_MaintainsAggregateRootInvariants()
    {
        // Act
        var project = Project.Create("Test", 0, "Description", 1, "user1");

        // Assert - Verify all collections are initialized
        project.Classes.Should().NotBeNull();
        project.Annotations.Should().NotBeNull();
        project.Trainings.Should().NotBeNull();
    }

    [Fact]
    public void AddClass_MaintainsClassIndexUniqueness()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");
        var class1 = ProjectClass.Create(project.Id, 0, "Person", "#FF0000");
        var class2 = ProjectClass.Create(project.Id, 1, "Car", "#00FF00");
        var class3 = ProjectClass.Create(project.Id, 0, "Duplicate", "#0000FF");  // Duplicate index

        // Act
        project.AddClass(class1);
        project.AddClass(class2);
        var act = () => project.AddClass(class3);

        // Assert
        act.Should().Throw<DuplicateEntityException>();
        project.Classes.Should().HaveCount(2);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var project = Project.Create("Test", 0, null, 1, "user1");

        // Act
        project.ClearDomainEvents();

        // Assert
        project.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DatasetId_IsSetCorrectly()
    {
        // Arrange
        int expectedDatasetId = 42;

        // Act
        var project = Project.Create("Test", 0, null, expectedDatasetId, "user1");

        // Assert
        project.DatasetId.Should().Be(expectedDatasetId);
    }

    [Fact]
    public void Type_ReturnsCorrectProjectType()
    {
        // Act
        var project = Project.Create("Test", 2, null, 1, "user1");

        // Assert
        project.Type.Should().Be(ProjectType.FromValue(2));
        project.Type.Name.Should().Be("Segmentation");
    }

    [Fact]
    public void Name_ReturnsProjectNameValueObject()
    {
        // Act
        var project = Project.Create("Test Project", 0, null, 1, "user1");

        // Assert
        project.Name.Should().BeOfType<ProjectName>();
        project.Name.Value.Should().Be("Test Project");
    }

    [Fact]
    public void Description_ReturnsDescriptionValueObject()
    {
        // Act
        var project = Project.Create("Test", 0, "Test description", 1, "user1");

        // Assert
        project.Description.Should().BeOfType<Description>();
        project.Description.Value.Should().Be("Test description");
    }

    #endregion
}
