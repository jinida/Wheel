using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for TrainingConfiguration entity configuration
    /// Verifies EF Core configuration including value object conversions, indexes, relationships
    /// </summary>
    public class TrainingConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public TrainingConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsTraining()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Training");
        }

        [Fact]
        public void Configuration_NameValueObjectConversion_Configured()
        {
            // CRITICAL: Verify TrainingName value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var property = entityType!.FindProperty("Name");

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Name should have value converter for TrainingName value object");
        }

        [Fact]
        public void Configuration_StatusValueObjectConversion_Configured()
        {
            // CRITICAL: Verify TrainingStatus value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var property = entityType!.FindProperty("Status");

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Status should have value converter for TrainingStatus value object");
        }

        [Fact]
        public void Configuration_NameMaxLength_Is200()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var property = entityType!.FindProperty("Name");

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(200);
        }

        [Fact]
        public void Configuration_ProjectForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Project is deleted, Trainings should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var fk = entityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Project));

            // Act & Assert
            fk.Should().NotBeNull();
            fk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_EvaluationsForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Training is deleted, Evaluations should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var navigation = entityType!.FindNavigation(nameof(Training.Evaluations));

            // Act & Assert
            navigation.Should().NotBeNull();
            navigation!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var idProperty = entityType!.FindProperty(nameof(Training.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_RequiredFields_Configured()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));

            // Act
            var projectId = entityType!.FindProperty(nameof(Training.ProjectId));
            var name = entityType.FindProperty("Name");
            var status = entityType.FindProperty("Status");
            var createdAt = entityType.FindProperty(nameof(Training.CreatedAt));

            // Assert
            projectId!.IsNullable.Should().BeFalse();
            name!.IsNullable.Should().BeFalse();
            status!.IsNullable.Should().BeFalse();
            createdAt!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_EndedAt_IsNullable()
        {
            // EndedAt is optional (null until training completes)
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var property = entityType!.FindProperty(nameof(Training.EndedAt));

            // Act & Assert
            property!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configuration_SingleIndexes_Configured()
        {
            // Verify single-column indexes for ProjectId, Status, CreatedAt
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var projectIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "ProjectId");
            var statusIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "Status");
            var createdAtIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "CreatedAt");

            // Assert
            projectIdIndex.Should().BeTrue("ProjectId should have an index");
            statusIndex.Should().BeTrue("Status should have an index");
            createdAtIndex.Should().BeTrue("CreatedAt should have an index");
        }

        [Fact]
        public void Configuration_CompositeIndex_ProjectIdAndStatus_Exists()
        {
            // CRITICAL: Composite index for query performance
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var compositeIndex = indexes.FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "ProjectId") &&
                i.Properties.Any(p => p.Name == "Status"));

            // Assert
            compositeIndex.Should().NotBeNull("Composite index on ProjectId and Status should exist");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Training));

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
