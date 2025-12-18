using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Events.TrainingEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for Training entity - CRITICAL: State machine integration
/// </summary>
public class TrainingTests
{
    #region Factory Method Tests

    [Fact]
    public void Start_ValidParameters_CreatesSuccessfully()
    {
        // Arrange
        int projectId = 1;
        string name = "Training Run 1";

        // Act
        var training = Training.Start(projectId, name);

        // Assert
        training.Should().NotBeNull();
        training.ProjectId.Should().Be(projectId);
        training.Name.Value.Should().Be(name);
        training.Status.Value.Should().Be(0);  // Pending
        training.Status.Name.Should().Be("Pending");
        training.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        training.EndedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Start_InvalidProjectId_ThrowsValidationException(int invalidProjectId)
    {
        // Act
        var act = () => Training.Start(invalidProjectId, "Test Training");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Project ID*positive*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Start_EmptyOrWhitespaceName_ThrowsValidationException(string invalidName)
    {
        // Act
        var act = () => Training.Start(1, invalidName);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Training name*required*");
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Start_RaisesTrainingStartedEvent()
    {
        // NOTE: The Training entity raises TrainingStartedEvent when status changes to Running,
        // not when the entity is first created via Start(). The Start() factory creates
        // a training in Pending status.

        // Arrange & Act
        var training = Training.Start(1, "Test");

        // Assert - No event raised on creation
        training.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateStatus_PendingToRunning_RaisesTrainingStartedEvent()
    {
        // Arrange
        var training = Training.Start(1, "Test");

        // Act
        training.UpdateStatus(1);  // Running

        // Assert
        var domainEvents = training.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TrainingStartedEvent>();
    }

    [Fact]
    public void Complete_RunningTraining_RaisesTrainingCompletedEvent()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();

        // Act
        training.Complete();

        // Assert
        var domainEvents = training.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TrainingCompletedEvent>();
    }

    [Fact]
    public void Fail_RunningTraining_RaisesTrainingFailedEvent()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();

        // Act
        training.Fail("Error occurred");

        // Assert
        var domainEvents = training.DomainEvents;
        var failedEvent = domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TrainingFailedEvent>().Subject;

        failedEvent.Reason.Should().Be("Error occurred");
    }

    #endregion

    #region State Machine - Valid Transitions

    [Fact]
    public void UpdateStatus_PendingToRunning_UpdatesSuccessfully()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.ClearDomainEvents();

        // Act
        training.UpdateStatus(1);  // Running

        // Assert
        training.Status.Value.Should().Be(1);
        training.Status.Name.Should().Be("Running");
    }

    [Fact]
    public void UpdateStatus_PendingToFailed_ThrowsInvalidTrainingStatusException()
    {
        // CRITICAL STATE MACHINE TEST - Pending can ONLY transition to Running
        // Arrange
        var training = Training.Start(1, "Test");  // Pending

        // Act
        var act = () => training.UpdateStatus(3);  // Pending → Failed (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*0*3*");
    }

    [Fact]
    public void UpdateStatus_RunningToCompleted_UpdatesSuccessfully()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();

        // Act
        training.UpdateStatus(2);  // Completed

        // Assert
        training.Status.Value.Should().Be(2);
        training.Status.Name.Should().Be("Completed");
        training.EndedAt.Should().NotBeNull();
        training.EndedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateStatus_RunningToFailed_UpdatesSuccessfully()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();

        // Act
        training.UpdateStatus(3);  // Failed

        // Assert
        training.Status.Value.Should().Be(3);
        training.Status.Name.Should().Be("Failed");
        training.EndedAt.Should().NotBeNull();
    }

    #endregion

    #region State Machine - Invalid Transitions

    [Fact]
    public void UpdateStatus_PendingToCompleted_ThrowsInvalidTrainingStatusException()
    {
        // CRITICAL STATE MACHINE TEST
        // Arrange
        var training = Training.Start(1, "Test");  // Pending

        // Act
        var act = () => training.UpdateStatus(2);  // Pending → Completed (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*0*2*");
    }

    [Fact]
    public void UpdateStatus_RunningToPending_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running

        // Act
        var act = () => training.UpdateStatus(0);  // Running → Pending (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*1*0*");
    }

    [Fact]
    public void UpdateStatus_CompletedToPending_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.UpdateStatus(2);  // Completed

        // Act
        var act = () => training.UpdateStatus(0);  // Completed → Pending (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*2*0*");
    }

    [Fact]
    public void UpdateStatus_CompletedToRunning_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.UpdateStatus(2);  // Completed

        // Act
        var act = () => training.UpdateStatus(1);  // Completed → Running (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*2*1*");
    }

    [Fact]
    public void UpdateStatus_CompletedToFailed_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.UpdateStatus(2);  // Completed

        // Act
        var act = () => training.UpdateStatus(3);  // Completed → Failed (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*2*3*");
    }

    [Fact]
    public void UpdateStatus_FailedToPending_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.UpdateStatus(3);  // Failed

        // Act
        var act = () => training.UpdateStatus(0);  // Failed → Pending (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*3*0*");
    }

    [Fact]
    public void UpdateStatus_FailedToRunning_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.UpdateStatus(3);  // Failed

        // Act
        var act = () => training.UpdateStatus(1);  // Failed → Running (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*3*1*");
    }

    [Fact]
    public void UpdateStatus_FailedToCompleted_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.UpdateStatus(3);  // Failed

        // Act
        var act = () => training.UpdateStatus(2);  // Failed → Completed (invalid)

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*3*2*");
    }

    #endregion

    #region Complete Method Tests

    [Fact]
    public void Complete_RunningTraining_CompletesSuccessfully()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();

        // Act
        training.Complete();

        // Assert
        training.Status.Value.Should().Be(2);  // Completed
        training.Status.Name.Should().Be("Completed");
        training.EndedAt.Should().NotBeNull();
        training.EndedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_PendingTraining_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");  // Pending

        // Act
        var act = () => training.Complete();

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*0*2*");
    }

    [Fact]
    public void Complete_CompletedTraining_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.Complete();  // Completed

        // Act
        var act = () => training.Complete();

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*2*2*");
    }

    [Fact]
    public void Complete_FailedTraining_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.Fail("Error");  // Failed

        // Act
        var act = () => training.Complete();

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*3*2*");
    }

    [Fact]
    public void Complete_RunningTraining_ContainsCorrectDurationInEvent()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();

        // Act
        training.Complete();

        // Assert
        var completedEvent = training.DomainEvents
            .OfType<TrainingCompletedEvent>()
            .Should().ContainSingle().Subject;

        completedEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        completedEvent.Duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Fail Method Tests

    [Fact]
    public void Fail_RunningTraining_FailsSuccessfully()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();
        string reason = "Out of memory error";

        // Act
        training.Fail(reason);

        // Assert
        training.Status.Value.Should().Be(3);  // Failed
        training.Status.Name.Should().Be("Failed");
        training.EndedAt.Should().NotBeNull();
        training.EndedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Fail_PendingTraining_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");  // Pending

        // Act
        var act = () => training.Fail("Error");

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*0*3*");
    }

    [Fact]
    public void Fail_CompletedTraining_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.Complete();  // Completed

        // Act
        var act = () => training.Fail("Error");

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*2*3*");
    }

    [Fact]
    public void Fail_FailedTraining_ThrowsInvalidTrainingStatusException()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.Fail("First error");  // Failed

        // Act
        var act = () => training.Fail("Second error");

        // Assert
        act.Should().Throw<InvalidTrainingStatusException>()
            .WithMessage("*cannot transition*3*3*");
    }

    [Fact]
    public void Fail_WithReason_EventContainsReason()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running
        training.ClearDomainEvents();
        string expectedReason = "GPU out of memory";

        // Act
        training.Fail(expectedReason);

        // Assert
        var failedEvent = training.DomainEvents
            .OfType<TrainingFailedEvent>()
            .Should().ContainSingle().Subject;

        failedEvent.Reason.Should().Be(expectedReason);
    }

    #endregion

    #region Evaluation Tests

    [Fact]
    public void AddEvaluation_ValidEvaluation_AddsSuccessfully()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        var evaluation = Evaluation.Create(training.Id, "evaluations/eval1.json", null);

        // Act
        training.AddEvaluation(evaluation);

        // Assert
        training.Evaluations.Should().ContainSingle();
        training.Evaluations.First().Should().Be(evaluation);
    }

    [Fact]
    public void AddEvaluation_MultipleEvaluations_AddsAllSuccessfully()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        var eval1 = Evaluation.Create(training.Id, "evaluations/eval1.json", "{\"accuracy\": 0.95}");
        var eval2 = Evaluation.Create(training.Id, "evaluations/eval2.json", "{\"precision\": 0.92}");

        // Act
        training.AddEvaluation(eval1);
        training.AddEvaluation(eval2);

        // Assert
        training.Evaluations.Should().HaveCount(2);
        training.Evaluations.Should().Contain(eval1);
        training.Evaluations.Should().Contain(eval2);
    }

    [Fact]
    public void AddEvaluation_NullEvaluation_ThrowsValidationException()
    {
        // Arrange
        var training = Training.Start(1, "Test");

        // Act
        var act = () => training.AddEvaluation(null!);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Evaluation*cannot be null*");
    }

    [Fact]
    public void Evaluations_ReturnsReadOnlyCollection()
    {
        // Arrange
        var training = Training.Start(1, "Test");

        // Act & Assert
        training.Evaluations.Should().BeAssignableTo<IReadOnlyCollection<Evaluation>>();
    }

    #endregion

    #region Edge Cases and Business Rules

    [Fact]
    public void Status_InitiallySetToPending()
    {
        // Arrange & Act
        var training = Training.Start(1, "Test");

        // Assert
        training.Status.Should().Be(TrainingStatus.Pending);
    }

    [Fact]
    public void EndedAt_InitiallyNull()
    {
        // Arrange & Act
        var training = Training.Start(1, "Test");

        // Assert
        training.EndedAt.Should().BeNull();
    }

    [Fact]
    public void EndedAt_SetWhenCompleted()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running

        // Act
        training.Complete();

        // Assert
        training.EndedAt.Should().NotBeNull();
        training.EndedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EndedAt_SetWhenFailed()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Running

        // Act
        training.Fail("Error");

        // Assert
        training.EndedAt.Should().NotBeNull();
        training.EndedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var training = Training.Start(1, "Test");
        training.UpdateStatus(1);  // Raises TrainingStartedEvent

        // Act
        training.ClearDomainEvents();

        // Assert
        training.DomainEvents.Should().BeEmpty();
    }

    #endregion
}
