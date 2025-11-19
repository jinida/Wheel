using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Image entity
    /// </summary>
    public class ImageConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.ToTable("Image");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                .ValueGeneratedOnAdd();

            builder.Property(i => i.Name)
                .HasMaxLength(50)
                .IsRequired();

            // Configure FilePath value object
            builder.Property(i => i.Path)
                .HasConversion(
                    v => v.Value,
                    v => FilePath.Create(v))
                .HasMaxLength(512)
                .IsRequired();

            builder.Property(i => i.DatasetId)
                .IsRequired();

            builder.Property(i => i.CreatedAt)
                .IsRequired();

            // Optimistic concurrency token
            builder.Property(i => i.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationships
            builder.HasMany(i => i.Annotations)
                .WithOne(a => a.Image)
                .HasForeignKey(a => a.ImageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(i => i.DomainEvents);

            // Indexes
            builder.HasIndex(i => i.DatasetId);

            builder.HasIndex(i => i.Name);

            builder.HasIndex(i => i.CreatedAt);

            // Composite index for dataset and name
            builder.HasIndex(i => new { i.DatasetId, i.Name });

            // Unique constraint on Path to prevent duplicate images
            // This prevents race conditions and ensures consistency
            builder.HasIndex(i => i.Path)
                .IsUnique()
                .HasDatabaseName("IX_Image_Path_Unique");
        }
    }
}
