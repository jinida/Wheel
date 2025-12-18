using FluentAssertions;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Events.EvaluationEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;
using Xunit;

namespace WheelApp.Tests.Domain.Entities;

/// <summary>
/// Tests for Evaluation entity - MEDIUM PRIORITY: Training evaluation results
/// </summary>
public class EvaluationTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_ValidEvaluation_CreatesSuccessfully()
    {
        // Arrange
        var trainingId = 1;
        var path = "evaluations/eval_001.json";
        var metricsJson = "{\"accuracy\": 0.95, \"loss\": 0.05}";

        // Act
        var evaluation = Evaluation.Create(trainingId, path, metricsJson);

        // Assert
        evaluation.Should().NotBeNull();
        evaluation.TrainingId.Should().Be(trainingId);
        evaluation.Path.Value.Should().Be(path);
        evaluation.MetricsJson.Should().Be(metricsJson);
        evaluation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullMetricsJson_CreatesSuccessfully()
    {
        // Arrange
        var trainingId = 1;
        var path = "evaluations/eval_001.json";

        // Act
        var evaluation = Evaluation.Create(trainingId, path, null);

        // Assert
        evaluation.Should().NotBeNull();
        evaluation.MetricsJson.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutMetricsJsonParameter_CreatesWithNull()
    {
        // Act
        var evaluation = Evaluation.Create(1, "evaluations/eval.json");

        // Assert
        evaluation.MetricsJson.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_InvalidTrainingId_ThrowsValidationException(int invalidTrainingId)
    {
        // Act
        var act = () => Evaluation.Create(invalidTrainingId, "evaluations/eval.json");

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*Training ID*cannot be negative*");
    }

    [Fact]
    public void Create_TrainingIdZero_AllowedForDomainTests()
    {
        // Domain tests create entities without DB persistence (Id=0)
        // Act
        var evaluation = Evaluation.Create(0, "evaluations/eval.json");

        // Assert
        evaluation.Should().NotBeNull();
        evaluation.TrainingId.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespacePath_ThrowsValidationException(string invalidPath)
    {
        // Act
        var act = () => Evaluation.Create(1, invalidPath);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*cannot be empty*");
    }

    [Fact]
    public void Create_PathExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var longPath = new string('A', 513);  // Max is 512

        // Act
        var act = () => Evaluation.Create(1, longPath);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*cannot exceed 512 characters*");
    }

    [Theory]
    [InlineData("evaluations/../eval.json")]
    [InlineData("evaluations//eval.json")]
    [InlineData("evaluations\\\\eval.json")]
    public void Create_PathWithInvalidSequences_ThrowsValidationException(string invalidPath)
    {
        // Act
        var act = () => Evaluation.Create(1, invalidPath);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*contains invalid sequences*");
    }

    [Theory]
    [InlineData("evaluations/eval<test>.json")]
    [InlineData("evaluations/eval>test.json")]
    [InlineData("evaluations/eval:test.json")]
    [InlineData("evaluations/eval\"test.json")]
    [InlineData("evaluations/eval|test.json")]
    [InlineData("evaluations/eval?test.json")]
    [InlineData("evaluations/eval*test.json")]
    public void Create_PathWithDangerousCharacters_ThrowsValidationException(string invalidPath)
    {
        // Act
        var act = () => Evaluation.Create(1, invalidPath);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("*File path*contains invalid characters*");
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void Create_RaisesEvaluationCreatedEvent()
    {
        // Act
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", "{\"accuracy\": 0.95}");

        // Assert
        var domainEvents = evaluation.DomainEvents;
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EvaluationCreatedEvent>();
    }

    [Fact]
    public void UpdateMetrics_RaisesEvaluationUpdatedEvent()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", "{\"accuracy\": 0.95}");
        evaluation.ClearDomainEvents();
        var newMetrics = "{\"accuracy\": 0.97}";

        // Act
        evaluation.UpdateMetrics(newMetrics);

        // Assert
        var domainEvents = evaluation.DomainEvents;
        var updatedEvent = domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EvaluationUpdatedEvent>().Subject;

        updatedEvent.OldMetrics.Should().Be("{\"accuracy\": 0.95}");
        updatedEvent.NewMetrics.Should().Be(newMetrics);
    }

    [Fact]
    public void UpdateMetrics_FromNullToValue_RaisesEvent()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", null);
        evaluation.ClearDomainEvents();

        // Act
        evaluation.UpdateMetrics("{\"accuracy\": 0.95}");

        // Assert
        var updatedEvent = evaluation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EvaluationUpdatedEvent>().Subject;

        updatedEvent.OldMetrics.Should().BeNull();
        updatedEvent.NewMetrics.Should().Be("{\"accuracy\": 0.95}");
    }

    [Fact]
    public void UpdateMetrics_FromValueToNull_RaisesEvent()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", "{\"accuracy\": 0.95}");
        evaluation.ClearDomainEvents();

        // Act
        evaluation.UpdateMetrics(null);

        // Assert
        var updatedEvent = evaluation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EvaluationUpdatedEvent>().Subject;

        updatedEvent.OldMetrics.Should().Be("{\"accuracy\": 0.95}");
        updatedEvent.NewMetrics.Should().BeNull();
    }

    #endregion

    #region UpdateMetrics Tests

    [Fact]
    public void UpdateMetrics_ValidMetrics_UpdatesSuccessfully()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", "{\"accuracy\": 0.95}");
        var newMetrics = "{\"accuracy\": 0.97, \"precision\": 0.96, \"recall\": 0.94}";

        // Act
        evaluation.UpdateMetrics(newMetrics);

        // Assert
        evaluation.MetricsJson.Should().Be(newMetrics);
    }

    [Fact]
    public void UpdateMetrics_ToNull_UpdatesSuccessfully()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", "{\"accuracy\": 0.95}");

        // Act
        evaluation.UpdateMetrics(null);

        // Assert
        evaluation.MetricsJson.Should().BeNull();
    }

    [Fact]
    public void UpdateMetrics_FromNull_UpdatesSuccessfully()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", null);

        // Act
        evaluation.UpdateMetrics("{\"accuracy\": 0.95}");

        // Assert
        evaluation.MetricsJson.Should().Be("{\"accuracy\": 0.95}");
    }

    [Fact]
    public void UpdateMetrics_EmptyString_UpdatesSuccessfully()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", "{\"accuracy\": 0.95}");

        // Act
        evaluation.UpdateMetrics("");

        // Assert
        evaluation.MetricsJson.Should().Be("");
    }

    #endregion

    #region CreatedAt Tests

    [Fact]
    public void CreatedAt_SetsDuringCreation()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", null);

        // Assert
        var afterCreate = DateTime.UtcNow;
        evaluation.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        evaluation.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_ComplexMetricsJson_CreatesSuccessfully()
    {
        // Arrange
        var complexMetrics = @"{
            ""accuracy"": 0.95,
            ""precision"": 0.94,
            ""recall"": 0.96,
            ""f1_score"": 0.95,
            ""confusion_matrix"": [[100, 5], [3, 92]],
            ""class_metrics"": {
                ""class_0"": {""precision"": 0.97, ""recall"": 0.95},
                ""class_1"": {""precision"": 0.93, ""recall"": 0.96}
            }
        }";

        // Act
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", complexMetrics);

        // Assert
        evaluation.MetricsJson.Should().Be(complexMetrics);
    }

    [Fact]
    public void Path_ReturnsFilePathValueObject()
    {
        // Act
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", null);

        // Assert
        evaluation.Path.Should().BeOfType<FilePath>();
        evaluation.Path.Value.Should().Be("evaluations/eval.json");
    }

    [Fact]
    public void TrainingId_IsSetCorrectly()
    {
        // Arrange
        var expectedTrainingId = 42;

        // Act
        var evaluation = Evaluation.Create(expectedTrainingId, "evaluations/eval.json", null);

        // Assert
        evaluation.TrainingId.Should().Be(expectedTrainingId);
    }

    [Theory]
    [InlineData("evaluations/eval_001.json")]
    [InlineData("results/training_1/evaluation.json")]
    [InlineData("data/metrics/run_42.json")]
    public void Create_VariousValidPaths_CreatesSuccessfully(string validPath)
    {
        // Act
        var evaluation = Evaluation.Create(1, validPath, null);

        // Assert
        evaluation.Path.Value.Should().Be(validPath);
    }

    [Theory]
    [InlineData("{\"accuracy\": 0.95}")]
    [InlineData("{\"loss\": 0.05, \"val_loss\": 0.06}")]
    [InlineData("[1, 2, 3, 4, 5]")]
    [InlineData("null")]
    [InlineData("\"metrics data\"")]
    public void Create_VariousJsonFormats_CreatesSuccessfully(string jsonData)
    {
        // Act
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", jsonData);

        // Assert
        evaluation.MetricsJson.Should().Be(jsonData);
    }

    [Fact]
    public void UpdateMetrics_MultipleTimes_MaintainsCorrectState()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", "{\"accuracy\": 0.95}");

        // Act
        evaluation.UpdateMetrics("{\"accuracy\": 0.96}");
        evaluation.UpdateMetrics("{\"accuracy\": 0.97}");
        evaluation.UpdateMetrics("{\"accuracy\": 0.98}");

        // Assert
        evaluation.MetricsJson.Should().Be("{\"accuracy\": 0.98}");
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", null);

        // Act
        evaluation.ClearDomainEvents();

        // Assert
        evaluation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_LargeMetricsJson_CreatesSuccessfully()
    {
        // Arrange
        var largeMetrics = "{\"data\": [" + string.Join(", ", Enumerable.Range(1, 1000).Select(x => $"{{\"value\": {x}}}")) + "]}";

        // Act
        var evaluation = Evaluation.Create(1, "evaluations/eval.json", largeMetrics);

        // Assert
        evaluation.MetricsJson.Should().Be(largeMetrics);
    }

    #endregion
}
