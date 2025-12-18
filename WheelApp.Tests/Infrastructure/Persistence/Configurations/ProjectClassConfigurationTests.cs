using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for ProjectClassConfiguration entity configuration
    /// Verifies EF Core configuration including composite unique indexes, custom column names, value object conversions
    /// </summary>
    public class ProjectClassConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public ProjectClassConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsClass()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Class");
        }

        [Fact]
        public void Configuration_CustomColumnNames_Configured()
        {
            // CRITICAL: Verify all custom column names are correctly configured
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));

            // Act
            var idColumn = entityType!.FindProperty(nameof(ProjectClass.Id))!.GetColumnName();
            var projectIdColumn = entityType.FindProperty(nameof(ProjectClass.ProjectId))!.GetColumnName();
            var classIdxColumn = entityType.FindProperty(nameof(ProjectClass.ClassIdx))!.GetColumnName();
            var nameColumn = entityType.FindProperty(nameof(ProjectClass.Name))!.GetColumnName();
            var colorColumn = entityType.FindProperty(nameof(ProjectClass.Color))!.GetColumnName();
            var rowVersionColumn = entityType.FindProperty("RowVersion")!.GetColumnName();

            // Assert
            idColumn.Should().Be("id");
            projectIdColumn.Should().Be("projectId");
            classIdxColumn.Should().Be("classIdx");
            nameColumn.Should().Be("name");
            colorColumn.Should().Be("color");
            rowVersionColumn.Should().Be("rowVersion");
        }

        [Fact]
        public void Configuration_CompositeUniqueIndex_ProjectIdAndClassIdx_Exists()
        {
            // CRITICAL: Unique constraint on ProjectId and ClassIdx
            // Ensures each class index is unique within a project
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var compositeIndex = indexes.FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "ProjectId") &&
                i.Properties.Any(p => p.Name == "ClassIdx"));

            // Assert
            compositeIndex.Should().NotBeNull("Composite index on ProjectId and ClassIdx should exist");
            compositeIndex!.IsUnique.Should().BeTrue("ProjectId and ClassIdx composite index should be unique");
        }

        [Fact]
        public void Configuration_ClassIdxValueObjectConversion_Configured()
        {
            // CRITICAL: Verify ClassIndex value object is converted to int for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var property = entityType!.FindProperty(nameof(ProjectClass.ClassIdx));

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("ClassIdx should have value converter for ClassIndex value object");
        }

        [Fact]
        public void Configuration_ColorValueObjectConversion_Configured()
        {
            // CRITICAL: Verify ColorCode value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var property = entityType!.FindProperty(nameof(ProjectClass.Color));

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Color should have value converter for ColorCode value object");
        }

        [Fact]
        public void Configuration_NameMaxLength_Is30()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var property = entityType!.FindProperty(nameof(ProjectClass.Name));

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(30);
        }

        [Fact]
        public void Configuration_ColorMaxLength_Is7()
        {
            // Color code format: #RRGGBB (7 characters)
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var property = entityType!.FindProperty(nameof(ProjectClass.Color));

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(7, "Color code should be 7 characters for #RRGGBB format");
        }

        [Fact]
        public void Configuration_ProjectForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Project is deleted, Classes should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var fk = entityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Project));

            // Act & Assert
            fk.Should().NotBeNull();
            fk!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_AnnotationsForeignKey_RestrictDelete()
        {
            // CRITICAL: Restrict delete behavior - cannot delete ProjectClass with existing Annotations
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var navigation = entityType!.FindNavigation(nameof(ProjectClass.Annotations));

            // Act & Assert
            navigation.Should().NotBeNull();
            navigation!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var idProperty = entityType!.FindProperty(nameof(ProjectClass.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_RequiredFields_Configured()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));

            // Act
            var projectId = entityType!.FindProperty(nameof(ProjectClass.ProjectId));
            var classIdx = entityType.FindProperty(nameof(ProjectClass.ClassIdx));
            var name = entityType.FindProperty(nameof(ProjectClass.Name));
            var color = entityType.FindProperty(nameof(ProjectClass.Color));

            // Assert
            projectId!.IsNullable.Should().BeFalse();
            classIdx!.IsNullable.Should().BeFalse();
            name!.IsNullable.Should().BeFalse();
            color!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_SingleIndexes_Configured()
        {
            // Verify single-column indexes for ProjectId, Name
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var projectIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "ProjectId");
            var nameIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "Name");

            // Assert
            projectIdIndex.Should().BeTrue("ProjectId should have an index");
            nameIndex.Should().BeTrue("Name should have an index");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(ProjectClass));

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
