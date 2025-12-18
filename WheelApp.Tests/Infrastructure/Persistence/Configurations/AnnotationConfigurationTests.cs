using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for AnnotationConfiguration entity configuration
    /// Verifies EF Core configuration including max lengths, cascade delete, indexes
    /// </summary>
    public class AnnotationConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public AnnotationConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsAnnotation()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Annotation");
        }

        [Fact]
        public void Configuration_InformationMaxLength_Is4000()
        {
            // CRITICAL: JSON storage limit
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var property = entityType!.FindProperty(nameof(Annotation.Information));

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(4000);
        }

        [Fact]
        public void Configuration_ImageForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Image is deleted, Annotations should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var fk = entityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Image));

            // Act & Assert
            fk.Should().NotBeNull();
            fk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_ProjectForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Project is deleted, Annotations should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var fk = entityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Project));

            // Act & Assert
            fk.Should().NotBeNull();
            fk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_ProjectClassForeignKey_RestrictDelete()
        {
            // CRITICAL: Restrict delete behavior - cannot delete ProjectClass with existing Annotations
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var fk = entityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(ProjectClass));

            // Act & Assert
            fk.Should().NotBeNull();
            fk!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var idProperty = entityType!.FindProperty(nameof(Annotation.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_RequiredFields_Configured()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));

            // Act
            var imageId = entityType!.FindProperty(nameof(Annotation.ImageId));
            var projectId = entityType.FindProperty(nameof(Annotation.ProjectId));
            var classId = entityType.FindProperty(nameof(Annotation.ClassId));
            var createdAt = entityType.FindProperty(nameof(Annotation.CreatedAt));

            // Assert
            imageId!.IsNullable.Should().BeFalse();
            projectId!.IsNullable.Should().BeFalse();
            classId!.IsNullable.Should().BeFalse();
            createdAt!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_Information_IsNullable()
        {
            // Information can be NULL for classifications
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var property = entityType!.FindProperty(nameof(Annotation.Information));

            // Act & Assert
            property!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configuration_CompositeIndex_ImageIdAndProjectId_Exists()
        {
            // CRITICAL: Composite index for query performance
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var imageProjectIndex = indexes.FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "ImageId") &&
                i.Properties.Any(p => p.Name == "ProjectId"));

            // Assert
            imageProjectIndex.Should().NotBeNull();
        }

        [Fact]
        public void Configuration_CompositeIndex_ProjectIdAndClassId_Exists()
        {
            // CRITICAL: Composite index for filtering annotations by project and class
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var projectClassIndex = indexes.FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "ProjectId") &&
                i.Properties.Any(p => p.Name == "ClassId"));

            // Assert
            projectClassIndex.Should().NotBeNull();
        }

        [Fact]
        public void Configuration_SingleIndexes_Configured()
        {
            // Verify single-column indexes for ImageId, ProjectId, ClassId, CreatedAt
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var imageIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "ImageId");
            var projectIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "ProjectId");
            var classIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "ClassId");
            var createdAtIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "CreatedAt");

            // Assert
            imageIdIndex.Should().BeTrue("ImageId should have an index");
            projectIdIndex.Should().BeTrue("ProjectId should have an index");
            classIdIndex.Should().BeTrue("ClassId should have an index");
            createdAtIndex.Should().BeTrue("CreatedAt should have an index");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));

            // Act
            var primaryKey = entityType!.FindPrimaryKey();

            // Assert
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().HaveCount(1);
            primaryKey.Properties.First().Name.Should().Be("Id");
        }

        [Fact]
        public void Configuration_AllForeignKeys_Configured()
        {
            // Verify all three foreign key relationships
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Annotation));
            var foreignKeys = entityType!.GetForeignKeys().ToList();

            // Act & Assert
            foreignKeys.Should().HaveCount(3, "Annotation should have 3 foreign keys: Image, Project, ProjectClass");

            var imageFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Image));
            var projectFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Project));
            var projectClassFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ProjectClass));

            imageFk.Should().NotBeNull();
            projectFk.Should().NotBeNull();
            projectClassFk.Should().NotBeNull();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
