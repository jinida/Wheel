using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Events.RoleEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for Role entity - MEDIUM PRIORITY: Training data split assignment
/// </summary>
public class RoleTests
{
    #region Factory Method Tests

    [Theory]
    [InlineData(0, "Train")]
    [InlineData(1, "Validation")]
    [InlineData(2, "Test")]
    [InlineData(3, "None")]
    public void Create_ValidRoleType_CreatesSuccessfully(int roleTypeValue, string expectedRoleName)
    {
        // Arrange
        var imageId = 1;
        var projectId = 2;

        // Act
        var role = Role.Create(imageId, projectId, roleTypeValue);

        // Assert
        role.Should().NotBeNull();
        role.ImageId.Should().Be(imageId);
        role.ProjectId.Should().Be(projectId);
        role.RoleType.Value.Should().Be(roleTypeValue);
        role.RoleType.Name.Should().Be(expectedRoleName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_InvalidImageId_ThrowsValidationException(int invalidImageId)
    {
        // Act
        var act = () => Role.Create(invalidImageId, 1, 0);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Image ID*cannot be negative*");
    }

    [Fact]
    public void Create_ImageIdZero_AllowedForDomainTests()
    {
        // Act
        var role = Role.Create(0, 1, 0);

        // Assert
        role.ImageId.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_InvalidProjectId_ThrowsValidationException(int invalidProjectId)
    {
        // Act
        var act = () => Role.Create(1, invalidProjectId, 0);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Project ID*cannot be negative*");
    }

    [Fact]
    public void Create_ProjectIdZero_AllowedForDomainTests()
    {
        // Act
        var role = Role.Create(1, 0, 0);

        // Assert
        role.ProjectId.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public void Create_InvalidRoleTypeValue_ThrowsValidationException(int invalidRoleType)
    {
        // Act
        var act = () => Role.Create(1, 1, invalidRoleType);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Invalid role type value*");
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Create_RaisesRoleAssignedEvent()
    {
        // Act
        var role = Role.Create(1, 2, 0);

        // Assert
        var domainEvents = role.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RoleAssignedEvent>();
    }

    [Fact]
    public void ChangeRole_RaisesRoleChangedEvent()
    {
        // Arrange
        var role = Role.Create(1, 2, 0);  // Train
        role.ClearDomainEvents();

        // Act
        role.ChangeRole(1);  // Change to Validation

        // Assert
        var domainEvents = role.DomainEvents;
        var changedEvent = domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RoleChangedEvent>().Subject;

        changedEvent.OldRoleType.Value.Should().Be(0);
        changedEvent.OldRoleType.Name.Should().Be("Train");
        changedEvent.NewRoleType.Value.Should().Be(1);
        changedEvent.NewRoleType.Name.Should().Be("Validation");
    }

    #endregion

    #region ChangeRole Tests

    [Fact]
    public void ChangeRole_ValidRoleType_ChangesSuccessfully()
    {
        // Arrange
        var role = Role.Create(1, 2, 0);  // Train
        var newRoleType = 1;  // Validation

        // Act
        role.ChangeRole(newRoleType);

        // Assert
        role.RoleType.Value.Should().Be(newRoleType);
        role.RoleType.Name.Should().Be("Validation");
    }

    [Theory]
    [InlineData(0, 1)]  // Train to Validation
    [InlineData(1, 2)]  // Validation to Test
    [InlineData(2, 3)]  // Test to None
    [InlineData(3, 0)]  // None to Train
    public void ChangeRole_VariousRoleTransitions_ChangesSuccessfully(int oldRoleType, int newRoleType)
    {
        // Arrange
        var role = Role.Create(1, 2, oldRoleType);

        // Act
        role.ChangeRole(newRoleType);

        // Assert
        role.RoleType.Value.Should().Be(newRoleType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public void ChangeRole_InvalidRoleType_ThrowsValidationException(int invalidRoleType)
    {
        // Arrange
        var role = Role.Create(1, 2, 0);

        // Act
        var act = () => role.ChangeRole(invalidRoleType);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Invalid role type value*");
    }

    [Fact]
    public void ChangeRole_ToSameRole_UpdatesSuccessfully()
    {
        // Arrange
        var role = Role.Create(1, 2, 0);  // Train
        role.ClearDomainEvents();

        // Act
        role.ChangeRole(0);  // Stay as Train

        // Assert
        role.RoleType.Value.Should().Be(0);
        role.RoleType.Name.Should().Be("Train");

        // Should still raise event even when same
        role.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RoleChangedEvent>();
    }

    #endregion

    #region RoleType Value Object Tests

    [Fact]
    public void RoleType_Train_HasCorrectValues()
    {
        // Act
        var role = Role.Create(1, 2, 0);

        // Assert
        role.RoleType.Should().Be(RoleType.Train);
        role.RoleType.Value.Should().Be(0);
        role.RoleType.Name.Should().Be("Train");
    }

    [Fact]
    public void RoleType_Validation_HasCorrectValues()
    {
        // Act
        var role = Role.Create(1, 2, 1);

        // Assert
        role.RoleType.Should().Be(RoleType.Validation);
        role.RoleType.Value.Should().Be(1);
        role.RoleType.Name.Should().Be("Validation");
    }

    [Fact]
    public void RoleType_Test_HasCorrectValues()
    {
        // Act
        var role = Role.Create(1, 2, 2);

        // Assert
        role.RoleType.Should().Be(RoleType.Test);
        role.RoleType.Value.Should().Be(2);
        role.RoleType.Name.Should().Be("Test");
    }

    [Fact]
    public void RoleType_None_HasCorrectValues()
    {
        // Act
        var role = Role.Create(1, 2, 3);

        // Assert
        role.RoleType.Should().Be(RoleType.None);
        role.RoleType.Value.Should().Be(3);
        role.RoleType.Name.Should().Be("None");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_MultipleRolesForDifferentImages_CreatesSuccessfully()
    {
        // Act
        var role1 = Role.Create(1, 1, 0);
        var role2 = Role.Create(2, 1, 1);
        var role3 = Role.Create(3, 1, 2);

        // Assert
        role1.RoleType.Name.Should().Be("Train");
        role2.RoleType.Name.Should().Be("Validation");
        role3.RoleType.Name.Should().Be("Test");
    }

    [Fact]
    public void Create_MultipleRolesForDifferentProjects_CreatesSuccessfully()
    {
        // Act
        var role1 = Role.Create(1, 1, 0);
        var role2 = Role.Create(1, 2, 1);
        var role3 = Role.Create(1, 3, 2);

        // Assert
        role1.ProjectId.Should().Be(1);
        role2.ProjectId.Should().Be(2);
        role3.ProjectId.Should().Be(3);
    }

    [Fact]
    public void ImageId_IsSetCorrectly()
    {
        // Arrange
        var expectedImageId = 42;

        // Act
        var role = Role.Create(expectedImageId, 1, 0);

        // Assert
        role.ImageId.Should().Be(expectedImageId);
    }

    [Fact]
    public void ProjectId_IsSetCorrectly()
    {
        // Arrange
        var expectedProjectId = 42;

        // Act
        var role = Role.Create(1, expectedProjectId, 0);

        // Assert
        role.ProjectId.Should().Be(expectedProjectId);
    }

    [Fact]
    public void RoleType_ReturnsRoleTypeValueObject()
    {
        // Act
        var role = Role.Create(1, 2, 0);

        // Assert
        role.RoleType.Should().BeOfType<RoleType>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var role = Role.Create(1, 2, 0);

        // Act
        role.ClearDomainEvents();

        // Assert
        role.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeRole_MultipleTimes_MaintainsCorrectState()
    {
        // Arrange
        var role = Role.Create(1, 2, 0);  // Train

        // Act
        role.ChangeRole(1);  // Validation
        role.ChangeRole(2);  // Test
        role.ChangeRole(3);  // None
        role.ChangeRole(0);  // Back to Train

        // Assert
        role.RoleType.Value.Should().Be(0);
        role.RoleType.Name.Should().Be("Train");
    }

    #endregion
}
