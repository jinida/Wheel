using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for DatasetConfiguration entity configuration
    /// Verifies EF Core configuration including unique constraints, value object conversions, max lengths
    /// </summary>
    public class DatasetConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public DatasetConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsDataset()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Dataset");
        }

        [Fact]
        public void Configuration_Name_HasUniqueIndex()
        {
            // CRITICAL: Unique constraint - dataset names must be unique
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var index = entityType!.GetIndexes()
                .FirstOrDefault(i => i.Properties.Any(p => p.Name == "Name"));

            // Act & Assert
            index.Should().NotBeNull();
            index!.IsUnique.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Name_HasValueConverter()
        {
            // CRITICAL: Value object conversion - DatasetName -> string
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var nameProperty = entityType!.FindProperty("Name");

            // Act
            var converter = nameProperty!.GetValueConverter();

            // Assert
            converter.Should().NotBeNull();
        }

        [Fact]
        public void Configuration_Name_MaxLength50()
        {
            // CRITICAL: Max length matches DatasetName value object constraint
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var nameProperty = entityType!.FindProperty("Name");

            // Act
            var maxLength = nameProperty!.GetMaxLength();

            // Assert
            maxLength.Should().Be(50);
        }

        [Fact]
        public void Configuration_Name_IsRequired()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var nameProperty = entityType!.FindProperty("Name");

            // Act & Assert
            nameProperty!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_Description_HasValueConverter()
        {
            // CRITICAL: Nullable value object conversion - Description -> string
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var descProperty = entityType!.FindProperty("Description");

            // Act
            var converter = descProperty!.GetValueConverter();

            // Assert
            converter.Should().NotBeNull();
        }

        [Fact]
        public void Configuration_Description_MaxLength255()
        {
            // CRITICAL: Max length matches Description value object constraint
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var descProperty = entityType!.FindProperty("Description");

            // Act
            var maxLength = descProperty!.GetMaxLength();

            // Assert
            maxLength.Should().Be(255);
        }

        [Fact]
        public void Configuration_Description_IsNullable()
        {
            // Description is optional
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var descProperty = entityType!.FindProperty("Description");

            // Act & Assert
            descProperty!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configuration_CreatedBy_MaxLength100()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var property = entityType!.FindProperty("CreatedBy");

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(100);
        }

        [Fact]
        public void Configuration_ModifiedBy_MaxLength100()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var property = entityType!.FindProperty("ModifiedBy");

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(100);
        }

        [Fact]
        public void Configuration_CreatedAt_IsRequired()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var property = entityType!.FindProperty("CreatedAt");

            // Act & Assert
            property!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_ModifiedAt_IsNullable()
        {
            // ModifiedAt is optional (only set when modified)
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var property = entityType!.FindProperty("ModifiedAt");

            // Act & Assert
            property!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_ImagesRelationship_CascadeDelete()
        {
            // CRITICAL: When Dataset is deleted, Images should be deleted
            // Arrange
            var imageEntityType = _context.Model.FindEntityType(typeof(Image));
            var datasetFk = imageEntityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Dataset));

            // Act & Assert
            datasetFk.Should().NotBeNull();
            datasetFk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_ProjectsRelationship_RestrictDelete()
        {
            // CRITICAL: Cannot delete dataset with projects - data protection
            // Arrange
            var projectEntityType = _context.Model.FindEntityType(typeof(Project));
            var datasetFk = projectEntityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Dataset));

            // Act & Assert
            datasetFk.Should().NotBeNull();
            datasetFk!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configuration_ImageCount_Ignored()
        {
            // CRITICAL: Computed property not mapped - calculated dynamically
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));

            // Act
            var imageCountProperty = entityType!.FindProperty("ImageCount");

            // Assert
            imageCountProperty.Should().BeNull("ImageCount is a computed property and should not be mapped");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_CreatedAtIndex_Exists()
        {
            // Index for sorting/filtering by creation date
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var createdAtIndex = indexes.Any(i =>
                i.Properties.Count == 1 &&
                i.Properties.First().Name == "CreatedAt");

            // Assert
            createdAtIndex.Should().BeTrue("CreatedAt should have an index for query performance");
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            var idProperty = entityType!.FindProperty(nameof(Dataset.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));

            // Act
            var primaryKey = entityType!.FindPrimaryKey();

            // Assert
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().HaveCount(1);
            primaryKey.Properties.First().Name.Should().Be("Id");
        }

        [Fact]
        public void Configuration_AllAuditFields_Configured()
        {
            // Verify all audit fields are properly configured
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));

            // Act
            var createdAt = entityType!.FindProperty("CreatedAt");
            var createdBy = entityType.FindProperty("CreatedBy");
            var modifiedAt = entityType.FindProperty("ModifiedAt");
            var modifiedBy = entityType.FindProperty("ModifiedBy");

            // Assert
            createdAt.Should().NotBeNull();
            createdBy.Should().NotBeNull();
            modifiedAt.Should().NotBeNull();
            modifiedBy.Should().NotBeNull();
        }

        [Fact]
        public void Configuration_NavigationProperties_Configured()
        {
            // Verify navigation properties exist
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Dataset));

            // Act
            var imagesNavigation = entityType!.FindNavigation("Images");
            var projectsNavigation = entityType.FindNavigation("Projects");

            // Assert
            imagesNavigation.Should().NotBeNull();
            projectsNavigation.Should().NotBeNull();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
