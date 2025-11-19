using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Project entity
    /// </summary>
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("Project");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            // Configure ProjectName value object
            builder.Property(p => p.Name)
                .HasConversion(
                    v => v.Value,
                    v => ProjectName.Create(v))
                .HasMaxLength(50)
                .IsRequired();

            // Configure ProjectType value object
            builder.Property(p => p.Type)
                .HasConversion(
                    v => v.Value,
                    v => ProjectType.FromValue(v))
                .IsRequired();

            // Configure Description value object - allow NULL
            builder.Property(p => p.Description)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => v != null ? Description.Create(v) : Description.Create(null))
                .HasMaxLength(255)
                .IsRequired(false);

            builder.Property(p => p.DatasetId)
                .IsRequired();

            // Audit fields
            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.CreatedBy)
                .HasMaxLength(100);

            builder.Property(p => p.ModifiedAt);

            builder.Property(p => p.ModifiedBy)
                .HasMaxLength(100);

            // Optimistic concurrency token
            builder.Property(p => p.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationships
            builder.HasOne(p => p.Dataset)
                .WithMany(d => d.Projects)
                .HasForeignKey(p => p.DatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Classes)
                .WithOne(c => c.Project)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Annotations)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Trainings)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(p => p.DomainEvents);

            // Indexes
            builder.HasIndex(p => p.Name);

            builder.HasIndex(p => p.DatasetId);

            builder.HasIndex(p => p.Type);

            builder.HasIndex(p => p.CreatedAt);
        }
    }
}
