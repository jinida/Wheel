using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for RoleConfiguration entity configuration
    /// Verifies EF Core configuration including composite unique constraint, value object conversion, indexes
    /// </summary>
    public class RoleConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public RoleConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsRole()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Role");
        }

        [Fact]
        public void Configuration_CompositeUniqueIndex_ImageIdAndProjectId_Exists()
        {
            // CRITICAL: Unique constraint on ImageId and ProjectId
            // Ensures each image can have only one role per project
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var uniqueIndex = indexes.FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "ImageId") &&
                i.Properties.Any(p => p.Name == "ProjectId"));

            // Assert
            uniqueIndex.Should().NotBeNull("Composite index on ImageId and ProjectId should exist");
            uniqueIndex!.IsUnique.Should().BeTrue("ImageId and ProjectId composite index should be unique");
        }

        [Fact]
        public void Configuration_RoleTypeValueObjectConversion_Configured()
        {
            // CRITICAL: Verify RoleType value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));
            var property = entityType!.FindProperty(nameof(Role.RoleType));

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("RoleType should have value converter for RoleType value object");
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));
            var idProperty = entityType!.FindProperty(nameof(Role.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_RequiredFields_Configured()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));

            // Act
            var imageId = entityType!.FindProperty(nameof(Role.ImageId));
            var projectId = entityType.FindProperty(nameof(Role.ProjectId));
            var roleType = entityType.FindProperty(nameof(Role.RoleType));

            // Assert
            imageId!.IsNullable.Should().BeFalse();
            projectId!.IsNullable.Should().BeFalse();
            roleType!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_SingleIndexes_Configured()
        {
            // Verify single-column indexes for ImageId, ProjectId, RoleType
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var imageIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "ImageId");
            var projectIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "ProjectId");
            var roleTypeIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "RoleType");

            // Assert
            imageIdIndex.Should().BeTrue("ImageId should have an index");
            projectIdIndex.Should().BeTrue("ProjectId should have an index");
            roleTypeIndex.Should().BeTrue("RoleType should have an index");
        }

        [Fact]
        public void Configuration_CompositeIndex_ProjectIdAndRoleType_Exists()
        {
            // CRITICAL: Composite index for query performance
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var compositeIndex = indexes.FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "ProjectId") &&
                i.Properties.Any(p => p.Name == "RoleType"));

            // Assert
            compositeIndex.Should().NotBeNull("Composite index on ProjectId and RoleType should exist");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Role));

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
