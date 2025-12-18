using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Entities;
using WheelApp.Infrastructure.Persistence;
using Xunit;

namespace WheelApp.Tests.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tests for ProjectConfiguration entity configuration
    /// Verifies EF Core configuration including value object conversions (ProjectName, ProjectType, Description)
    /// </summary>
    public class ProjectConfigurationTests : IDisposable
    {
        private readonly WheelAppDbContext _context;

        public ProjectConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<WheelAppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WheelAppDbContext(options);
        }

        [Fact]
        public void Configuration_TableName_IsProject()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));

            // Act
            var tableName = entityType!.GetTableName();

            // Assert
            tableName.Should().Be("Project");
        }

        [Fact]
        public void Configuration_NameValueObjectConversion_Configured()
        {
            // CRITICAL: Verify ProjectName value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty("Name");

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Name should have value converter for ProjectName value object");
        }

        [Fact]
        public void Configuration_TypeValueObjectConversion_Configured()
        {
            // CRITICAL: Verify ProjectType value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty("Type");

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Type should have value converter for ProjectType value object");
        }

        [Fact]
        public void Configuration_DescriptionValueObjectConversion_Configured()
        {
            // CRITICAL: Verify Description value object is converted to string for storage
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty("Description");

            // Act
            var valueConverter = property!.GetValueConverter();

            // Assert
            valueConverter.Should().NotBeNull("Description should have value converter for Description value object");
        }

        [Fact]
        public void Configuration_NameMaxLength_Is50()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty("Name");

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(50);
        }

        [Fact]
        public void Configuration_DescriptionMaxLength_Is255()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty("Description");

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(255);
        }

        [Fact]
        public void Configuration_CreatedByMaxLength_Is100()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty(nameof(Project.CreatedBy));

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(100);
        }

        [Fact]
        public void Configuration_ModifiedByMaxLength_Is100()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty(nameof(Project.ModifiedBy));

            // Act
            var maxLength = property!.GetMaxLength();

            // Assert
            maxLength.Should().Be(100);
        }

        [Fact]
        public void Configuration_DatasetForeignKey_RestrictDelete()
        {
            // CRITICAL: Restrict delete behavior - cannot delete Dataset with existing Projects
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var fk = entityType!.GetForeignKeys()
                .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Dataset));

            // Act & Assert
            fk.Should().NotBeNull();
            fk!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configuration_ClassesForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Project is deleted, Classes should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var navigation = entityType!.FindNavigation(nameof(Project.Classes));

            // Act & Assert
            navigation.Should().NotBeNull();
            navigation!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_AnnotationsForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Project is deleted, Annotations should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var navigation = entityType!.FindNavigation(nameof(Project.Annotations));

            // Act & Assert
            navigation.Should().NotBeNull();
            navigation!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_TrainingsForeignKey_CascadeDelete()
        {
            // CRITICAL: Cascade delete behavior - when Project is deleted, Trainings should be deleted
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var navigation = entityType!.FindNavigation(nameof(Project.Trainings));

            // Act & Assert
            navigation.Should().NotBeNull();
            navigation!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }

        [Fact]
        public void Configuration_RowVersion_IsConcurrencyToken()
        {
            // CRITICAL: Optimistic concurrency control
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var rowVersion = entityType!.FindProperty("RowVersion");

            // Act & Assert
            rowVersion.Should().NotBeNull();
            rowVersion!.IsConcurrencyToken.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Id_ValueGeneratedOnAdd()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var idProperty = entityType!.FindProperty(nameof(Project.Id));

            // Act
            var valueGenerated = idProperty!.ValueGenerated;

            // Assert
            valueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configuration_RequiredFields_Configured()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));

            // Act
            var name = entityType!.FindProperty("Name");
            var type = entityType.FindProperty("Type");
            var datasetId = entityType.FindProperty(nameof(Project.DatasetId));
            var createdAt = entityType.FindProperty(nameof(Project.CreatedAt));

            // Assert
            name!.IsNullable.Should().BeFalse();
            type!.IsNullable.Should().BeFalse();
            datasetId!.IsNullable.Should().BeFalse();
            createdAt!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configuration_Description_IsNullable()
        {
            // Description is optional
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty("Description");

            // Act & Assert
            property!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configuration_ModifiedAt_IsNullable()
        {
            // ModifiedAt is optional (null until first modification)
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var property = entityType!.FindProperty(nameof(Project.ModifiedAt));

            // Act & Assert
            property!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configuration_SingleIndexes_Configured()
        {
            // Verify single-column indexes for Name, DatasetId, Type, CreatedAt
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));
            var indexes = entityType!.GetIndexes().ToList();

            // Act
            var nameIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "Name");
            var datasetIdIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "DatasetId");
            var typeIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "Type");
            var createdAtIndex = indexes.Any(i => i.Properties.Count == 1 && i.Properties.First().Name == "CreatedAt");

            // Assert
            nameIndex.Should().BeTrue("Name should have an index");
            datasetIdIndex.Should().BeTrue("DatasetId should have an index");
            typeIndex.Should().BeTrue("Type should have an index");
            createdAtIndex.Should().BeTrue("CreatedAt should have an index");
        }

        [Fact]
        public void Configuration_DomainEvents_Ignored()
        {
            // Domain events should not be persisted to the database
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));

            // Act
            var domainEventsProperty = entityType!.FindProperty("DomainEvents");

            // Assert
            domainEventsProperty.Should().BeNull("DomainEvents should be ignored and not mapped");
        }

        [Fact]
        public void Configuration_PrimaryKey_IsId()
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(typeof(Project));

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
