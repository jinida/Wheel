using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for EvaluationConfiguration entity configuration
    /// Verifies EF Core configuration including value object conversion, max lengths, indexes
    /// </summary>
    public class EvaluationConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public EvaluationConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsEvaluation()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Evaluation");
        }

        [Fact]
        public void Configuration_PathValueObjectConversion_Configured()
        {
            // CRITICAL: Verify FilePath value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var property = entityType!.FindProperty("Path");

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Path should have value converter for FilePath value object");
        }

        [Fact]
        public void Configuration_PathMaxLength_Is512()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var property = entityType!.FindProperty("Path");

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(512, "Path should have max length of 512 for file paths");
        }

        [Fact]
        public void Configuration_MetricsJsonMaxLength_Is4000()
        {
            // CRITICAL: JSON storage limit for evaluation metrics
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var property = entityType!.FindProperty(nameof(Evaluation.MetricsJson));

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(4000, "MetricsJson should have max length of 4000 for JSON storage");
        }

        [Fact]
        public void Configuration_TrainingForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Training is deleted, Evaluations should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var fk = entityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Training));

            // Act & Assert
            fk.Should().NotBeNull();
            fk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var idProperty = entityType!.FindProperty(nameof(Evaluation.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_RequiredFields_Configured()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));

            // Act
            var trainingId = entityType!.FindProperty(nameof(Evaluation.TrainingId));
            var path = entityType.FindProperty("Path");
            var createdAt = entityType.FindProperty(nameof(Evaluation.CreatedAt));

            // Assert
            trainingId!.IsNullable.Should().BeFalse();
            path!.IsNullable.Should().BeFalse();
            createdAt!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_MetricsJson_IsNullable()
        {
            // MetricsJson is optional
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var property = entityType!.FindProperty(nameof(Evaluation.MetricsJson));

            // Act & Assert
            property!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configuration_SingleIndexes_Configured()
        {
            // Verify single-column indexes for TrainingId, CreatedAt
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var trainingIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "TrainingId");
            var createdAtIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "CreatedAt");

            // Assert
            trainingIdIndex.Should().BeTrue("TrainingId should have an index");
            createdAtIndex.Should().BeTrue("CreatedAt should have an index");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Evaluation));

            // Act
            var primaryKey = entityType!.FindPrimaryKey();

            // Assert
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().HaveCount(1);
            primaryKey.Properties.First().Name.Should().Be("Id");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
