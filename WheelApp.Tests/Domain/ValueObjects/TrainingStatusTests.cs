using FluentAssertions;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.ValueObjects
{
    /// <summary>
    /// CRITICAL STATE MACHINE TESTS: Training status transitions and validation
    /// </summary>
    public class TrainingStatusTests
    {
        [Theory]
        [InlineData(0, "Pending")]
        [InlineData(1, "Running")]
        [InlineData(2, "Completed")]
        [InlineData(3, "Failed")]
        public void FromValue_ValidStatus_CreatesSuccessfully(int value, string expectedName)
        {
            // Act
            var status = TrainingStatus.FromValue(value);

            // Assert
            status.Should().NotBeNull();
            status.Value.Should().Be(value);
            status.Name.Should().Be(expectedName);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void FromValue_InvalidStatus_ThrowsInvalidTrainingStatusException(int value)
        {
            // Act
            var act = () => TrainingStatus.FromValue(value);

            // Assert
            act.Should().Throw<InvalidTrainingStatusException>()
                .WithMessage("*not valid*")
                .And.AttemptedStatus.Should().Be(value);
        }

        [Fact]
        public void Pending_ReturnsCorrectStatus()
        {
            // Act
            var status = TrainingStatus.Pending;

            // Assert
            status.Value.Should().Be(0);
            status.Name.Should().Be("Pending");
        }

        [Fact]
        public void Running_ReturnsCorrectStatus()
        {
            // Act
            var status = TrainingStatus.Running;

            // Assert
            status.Value.Should().Be(1);
            status.Name.Should().Be("Running");
        }

        [Fact]
        public void Completed_ReturnsCorrectStatus()
        {
            // Act
            var status = TrainingStatus.Completed;

            // Assert
            status.Value.Should().Be(2);
            status.Name.Should().Be("Completed");
        }

        [Fact]
        public void Failed_ReturnsCorrectStatus()
        {
            // Act
            var status = TrainingStatus.Failed;

            // Assert
            status.Value.Should().Be(3);
            status.Name.Should().Be("Failed");
        }

        [Theory]
        [InlineData(0, 1, true)]   // Pending → Running ✅
        [InlineData(0, 2, false)]  // Pending → Completed ❌
        [InlineData(0, 3, false)]  // Pending → Failed ❌
        [InlineData(1, 2, true)]   // Running → Completed ✅
        [InlineData(1, 3, true)]   // Running → Failed ✅
        [InlineData(1, 0, false)]  // Running → Pending ❌
        [InlineData(2, 0, false)]  // Completed → Pending ❌
        [InlineData(2, 1, false)]  // Completed → Running ❌
        [InlineData(2, 3, false)]  // Completed → Failed ❌
        [InlineData(3, 0, false)]  // Failed → Pending ❌
        [InlineData(3, 1, false)]  // Failed → Running ❌
        [InlineData(3, 2, false)]  // Failed → Completed ❌
        public void CanTransitionTo_VariousTransitions_ReturnsExpected(int fromValue, int toValue, bool expected)
        {
            // CRITICAL STATE MACHINE TEST - Validates all state transitions
            // Arrange
            var fromStatus = TrainingStatus.FromValue(fromValue);
            var toStatus = TrainingStatus.FromValue(toValue);

            // Act
            var canTransition = fromStatus.CanTransitionTo(toStatus);

            // Assert
            canTransition.Should().Be(expected);
        }

        [Fact]
        public void CanTransitionTo_SameStatus_ReturnsFalse()
        {
            // Arrange
            var status = TrainingStatus.Running;

            // Act
            var canTransition = status.CanTransitionTo(status);

            // Assert
            canTransition.Should().BeFalse();
        }

        [Fact]
        public void CanTransitionTo_PendingToRunning_ReturnsTrue()
        {
            // Arrange
            var pending = TrainingStatus.Pending;
            var running = TrainingStatus.Running;

            // Act
            var canTransition = pending.CanTransitionTo(running);

            // Assert
            canTransition.Should().BeTrue();
        }

        [Fact]
        public void CanTransitionTo_RunningToCompleted_ReturnsTrue()
        {
            // Arrange
            var running = TrainingStatus.Running;
            var completed = TrainingStatus.Completed;

            // Act
            var canTransition = running.CanTransitionTo(completed);

            // Assert
            canTransition.Should().BeTrue();
        }

        [Fact]
        public void CanTransitionTo_RunningToFailed_ReturnsTrue()
        {
            // Arrange
            var running = TrainingStatus.Running;
            var failed = TrainingStatus.Failed;

            // Act
            var canTransition = running.CanTransitionTo(failed);

            // Assert
            canTransition.Should().BeTrue();
        }

        [Fact]
        public void CanTransitionTo_CompletedToAnyStatus_ReturnsFalse()
        {
            // CRITICAL: Completed is a terminal state
            // Arrange
            var completed = TrainingStatus.Completed;

            // Act & Assert
            completed.CanTransitionTo(TrainingStatus.Pending).Should().BeFalse();
            completed.CanTransitionTo(TrainingStatus.Running).Should().BeFalse();
            completed.CanTransitionTo(TrainingStatus.Failed).Should().BeFalse();
            completed.CanTransitionTo(TrainingStatus.Completed).Should().BeFalse();
        }

        [Fact]
        public void CanTransitionTo_FailedToAnyStatus_ReturnsFalse()
        {
            // CRITICAL: Failed is a terminal state
            // Arrange
            var failed = TrainingStatus.Failed;

            // Act & Assert
            failed.CanTransitionTo(TrainingStatus.Pending).Should().BeFalse();
            failed.CanTransitionTo(TrainingStatus.Running).Should().BeFalse();
            failed.CanTransitionTo(TrainingStatus.Completed).Should().BeFalse();
            failed.CanTransitionTo(TrainingStatus.Failed).Should().BeFalse();
        }

        [Fact]
        public void GetAll_ReturnsAllStatuses()
        {
            // Act
            var allStatuses = TrainingStatus.GetAll().ToList();

            // Assert
            allStatuses.Should().HaveCount(4);
            allStatuses.Should().Contain(s => s.Value == 0 && s.Name == "Pending");
            allStatuses.Should().Contain(s => s.Value == 1 && s.Name == "Running");
            allStatuses.Should().Contain(s => s.Value == 2 && s.Name == "Completed");
            allStatuses.Should().Contain(s => s.Value == 3 && s.Name == "Failed");
        }

        [Fact]
        public void Equals_SameValue_ReturnsTrue()
        {
            // Arrange
            var status1 = TrainingStatus.FromValue(1);
            var status2 = TrainingStatus.FromValue(1);

            // Act & Assert
            status1.Should().Be(status2);
            (status1 == status2).Should().BeTrue();
            status1.GetHashCode().Should().Be(status2.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_ReturnsFalse()
        {
            // Arrange
            var status1 = TrainingStatus.Pending;
            var status2 = TrainingStatus.Running;

            // Act & Assert
            status1.Should().NotBe(status2);
            (status1 == status2).Should().BeFalse();
        }

        [Fact]
        public void ToString_ReturnsName()
        {
            // Arrange
            var status = TrainingStatus.Running;

            // Act
            var result = status.ToString();

            // Assert
            result.Should().Be("Running");
        }

        [Fact]
        public void ImplicitConversion_ToInt_WorksCorrectly()
        {
            // Arrange
            var status = TrainingStatus.Running;

            // Act
            int result = status;

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void Equals_StaticPropertiesWithSameValue_ReturnsTrue()
        {
            // Arrange
            var pending1 = TrainingStatus.Pending;
            var pending2 = TrainingStatus.Pending;

            // Act & Assert
            pending1.Should().Be(pending2);
        }

        [Fact]
        public void Equals_StaticPropertyWithFromValue_ReturnsTrue()
        {
            // Arrange
            var pending1 = TrainingStatus.Pending;
            var pending2 = TrainingStatus.FromValue(0);

            // Act & Assert
            pending1.Should().Be(pending2);
        }

        [Fact]
        public void CanTransitionTo_PendingToCompleted_ReturnsFalse()
        {
            // CRITICAL: Cannot skip Running state
            // Arrange
            var pending = TrainingStatus.Pending;
            var completed = TrainingStatus.Completed;

            // Act
            var canTransition = pending.CanTransitionTo(completed);

            // Assert
            canTransition.Should().BeFalse();
        }

        [Fact]
        public void CanTransitionTo_PendingToFailed_ReturnsFalse()
        {
            // Arrange - Training must start running before it can fail
            var pending = TrainingStatus.Pending;
            var failed = TrainingStatus.Failed;

            // Act
            var canTransition = pending.CanTransitionTo(failed);

            // Assert
            canTransition.Should().BeFalse();
        }
    }
}
