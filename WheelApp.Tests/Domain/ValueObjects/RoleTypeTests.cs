using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects;

public class RoleTypeTests
{
    [Theory]
    [InlineData(0, "Train")]
    [InlineData(1, "Validation")]
    [InlineData(2, "Test")]
    [InlineData(3, "None")]
    public void FromValue_ValidRole_CreatesSuccessfully(int value, string expectedName)
    {
        // Act
        var roleType = RoleType.FromValue(value);

        // Assert
        roleType.Should().NotBeNull();
        roleType.Value.Should().Be(value);
        roleType.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void FromValue_InvalidRole_ThrowsValidationException(int value)
    {
        // Act
        var act = () => RoleType.FromValue(value);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage($"*{value}*");
    }

    [Fact]
    public void Train_ReturnsCorrectValues()
    {
        // Act
        var roleType = RoleType.Train;

        // Assert
        roleType.Value.Should().Be(0);
        roleType.Name.Should().Be("Train");
    }

    [Fact]
    public void Validation_ReturnsCorrectValues()
    {
        // Act
        var roleType = RoleType.Validation;

        // Assert
        roleType.Value.Should().Be(1);
        roleType.Name.Should().Be("Validation");
    }

    [Fact]
    public void Test_ReturnsCorrectValues()
    {
        // Act
        var roleType = RoleType.Test;

        // Assert
        roleType.Value.Should().Be(2);
        roleType.Name.Should().Be("Test");
    }

    [Fact]
    public void None_ReturnsCorrectValues()
    {
        // Act
        var roleType = RoleType.None;

        // Assert
        roleType.Value.Should().Be(3);
        roleType.Name.Should().Be("None");
    }

    [Fact]
    public void GetAll_ReturnsAllRoleTypes()
    {
        // Act
        var allRoles = RoleType.GetAll().ToList();

        // Assert
        allRoles.Should().HaveCount(4);
        allRoles.Should().Contain(r => r.Value == 0 && r.Name == "Train");
        allRoles.Should().Contain(r => r.Value == 1 && r.Name == "Validation");
        allRoles.Should().Contain(r => r.Value == 2 && r.Name == "Test");
        allRoles.Should().Contain(r => r.Value == 3 && r.Name == "None");
    }

    [Fact]
    public void Equals_SameRole_ReturnsTrue()
    {
        // Arrange
        var role1 = RoleType.FromValue(0);
        var role2 = RoleType.FromValue(0);

        // Act & Assert
        role1.Should().Be(role2);
        (role1 == role2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentRole_ReturnsFalse()
    {
        // Arrange
        var role1 = RoleType.FromValue(0);
        var role2 = RoleType.FromValue(1);

        // Act & Assert
        role1.Should().NotBe(role2);
        (role1 == role2).Should().BeFalse();
    }

    [Fact]
    public void Equals_StaticPropertyAndFromValue_ReturnsTrue()
    {
        // Arrange
        var role1 = RoleType.Train;
        var role2 = RoleType.FromValue(0);

        // Act & Assert
        role1.Should().Be(role2);
    }

    [Fact]
    public void GetHashCode_SameRole_ReturnsSameHashCode()
    {
        // Arrange
        var role1 = RoleType.FromValue(1);
        var role2 = RoleType.FromValue(1);

        // Act & Assert
        role1.GetHashCode().Should().Be(role2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        // Arrange
        var roleType = RoleType.Train;

        // Act
        var result = roleType.ToString();

        // Assert
        result.Should().Be("Train");
    }

    [Fact]
    public void ImplicitOperator_ConvertsToInt()
    {
        // Arrange
        var roleType = RoleType.Validation;

        // Act
        int result = roleType;

        // Assert
        result.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FromValue_MultipleCallsSameValue_ReturnsSameInstance(int value)
    {
        // Act
        var role1 = RoleType.FromValue(value);
        var role2 = RoleType.FromValue(value);

        // Assert
        role1.Should().Be(role2);
        ReferenceEquals(role1, role2).Should().BeTrue();
    }
}
