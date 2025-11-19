using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for ProjectClass entity
    /// </summary>
    public class ProjectClassConfiguration : IEntityTypeConfiguration<ProjectClass>
    {
        public void Configure(EntityTypeBuilder<ProjectClass> builder)
        {
            builder.ToTable("Class");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(c => c.ProjectId)
                .HasColumnName("projectId")
                .IsRequired();

            // Configure ClassIndex value object
            builder.Property(c => c.ClassIdx)
                .HasColumnName("classIdx")
                .HasConversion(
                    v => v.Value,
                    v => ClassIndex.Create(v))
                .IsRequired();

            builder.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(30)
                .IsRequired();

            // Configure ColorCode value object
            builder.Property(c => c.Color)
                .HasColumnName("color")
                .HasConversion(
                    v => v.Value,
                    v => ColorCode.Create(v))
                .HasMaxLength(7)
                .IsRequired();

            // Optimistic concurrency token
            builder.Property(c => c.RowVersion)
                .HasColumnName("rowVersion")
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationships
            builder.HasOne(c => c.Project)
                .WithMany(p => p.Classes)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Annotations)
                .WithOne(a => a.ProjectClass)
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore domain events
            builder.Ignore(c => c.DomainEvents);

            // Indexes
            builder.HasIndex(c => c.ProjectId);

            builder.HasIndex(c => c.Name);

            // Unique constraint on ProjectId and ClassIdx
            builder.HasIndex(c => new { c.ProjectId, c.ClassIdx })
                .IsUnique();
        }
    }
}
