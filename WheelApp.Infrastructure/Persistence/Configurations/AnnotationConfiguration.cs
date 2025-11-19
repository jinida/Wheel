using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Annotation entity
    /// </summary>
    public class AnnotationConfiguration : IEntityTypeConfiguration<Annotation>
    {
        public void Configure(EntityTypeBuilder<Annotation> builder)
        {
            builder.ToTable("Annotation");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .ValueGeneratedOnAdd();

            builder.Property(a => a.ImageId)
                .IsRequired();

            builder.Property(a => a.ProjectId)
                .IsRequired();

            builder.Property(a => a.ClassId)
                .IsRequired();

            // Information stores JSON coordinate array or NULL
            builder.Property(a => a.Information)
                .HasMaxLength(4000);

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            // Optimistic concurrency token
            builder.Property(a => a.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationships
            builder.HasOne(a => a.Image)
                .WithMany(i => i.Annotations)
                .HasForeignKey(a => a.ImageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Project)
                .WithMany(p => p.Annotations)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.ProjectClass)
                .WithMany(c => c.Annotations)
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore domain events
            builder.Ignore(a => a.DomainEvents);

            // Indexes
            builder.HasIndex(a => a.ImageId);

            builder.HasIndex(a => a.ProjectId);

            builder.HasIndex(a => a.ClassId);

            builder.HasIndex(a => a.CreatedAt);

            // Composite indexes
            builder.HasIndex(a => new { a.ImageId, a.ProjectId });

            builder.HasIndex(a => new { a.ProjectId, a.ClassId });
        }
    }
}
