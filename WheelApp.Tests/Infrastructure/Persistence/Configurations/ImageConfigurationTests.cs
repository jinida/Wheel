using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for ImageConfiguration entity configuration
    /// Verifies EF Core configuration including unique Path constraint, value object conversion, indexes
    /// </summary>
    public class ImageConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public ImageConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsImage()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Image");
        }

        [Fact]
        public void Configuration_PathHasUniqueIndex()
        {
            // CRITICAL: Unique constraint on Path to prevent duplicate images
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var index = entityType!.GetIndexes()
                .FirstOrDefault(i => i.Properties.Count == 1 && i.Properties.First().Name == "Path");

            // Act & Assert
            index.Should().NotBeNull("Path should have an index");
            index!.IsUnique.Should().BeTrue("Path index should be unique to prevent duplicate images");
        }

        [Fact]
        public void Configuration_PathUniqueIndex_HasCorrectDatabaseName()
        {
            // CRITICAL: Named index for unique Path constraint
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var index = entityType!.GetIndexes()
                .FirstOrDefault(i => i.Properties.Count == 1 && i.Properties.First().Name == "Path");

            // Act
            var databaseName = index!.GetDatabaseName();

            // Assert
            databaseName.Should().Be("IX_Image_Path_Unique");
        }

        [Fact]
        public void Configuration_PathMaxLength_Is512()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var property = entityType!.FindProperty("Path");

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(512, "Path should have max length of 512 for file paths");
        }

        [Fact]
        public void Configuration_NameMaxLength_Is50()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var property = entityType!.FindProperty(nameof(Image.Name));

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(50);
        }

        [Fact]
        public void Configuration_PathValueObjectConversion_Configured()
        {
            // CRITICAL: Verify FilePath value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var property = entityType!.FindProperty("Path");

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Path should have value converter for FilePath value object");
        }

        [Fact]
        public void Configuration_AnnotationsForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Image is deleted, Annotations should be deleted
            // This is verified from the Annotation side of the relationship
            // Arrange
            var imageType = _context.Model.FindEntityType(typeof(Image));
            var navigation = imageType!.FindNavigation(nameof(Image.Annotations));

            // Act & Assert
            navigation.Should().NotBeNull();
            navigation!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var idProperty = entityType!.FindProperty(nameof(Image.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_RequiredFields_Configured()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));

            // Act
            var name = entityType!.FindProperty(nameof(Image.Name));
            var path = entityType.FindProperty("Path");
            var datasetId = entityType.FindProperty(nameof(Image.DatasetId));
            var createdAt = entityType.FindProperty(nameof(Image.CreatedAt));

            // Assert
            name!.IsNullable.Should().BeFalse();
            path!.IsNullable.Should().BeFalse();
            datasetId!.IsNullable.Should().BeFalse();
            createdAt!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_SingleIndexes_Configured()
        {
            // Verify single-column indexes for DatasetId, Name, CreatedAt
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var datasetIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "DatasetId");
            var nameIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "Name");
            var createdAtIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "CreatedAt");

            // Assert
            datasetIdIndex.Should().BeTrue("DatasetId should have an index");
            nameIndex.Should().BeTrue("Name should have an index");
            createdAtIndex.Should().BeTrue("CreatedAt should have an index");
        }

        [Fact]
        public void Configuration_CompositeIndex_DatasetIdAndName_Exists()
        {
            // CRITICAL: Composite index for query performance
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var compositeIndex = indexes.FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "DatasetId") &&
                i.Properties.Any(p => p.Name == "Name"));

            // Assert
            compositeIndex.Should().NotBeNull("Composite index on DatasetId and Name should exist");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Image));

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
