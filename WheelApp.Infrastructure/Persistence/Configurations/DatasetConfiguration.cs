using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Dataset entity
    /// </summary>
    public class DatasetConfiguration : IEntityTypeConfiguration<Dataset>
    {
        public void Configure(EntityTypeBuilder<Dataset> builder)
        {
            builder.ToTable("Dataset");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                .ValueGeneratedOnAdd();

            // Configure DatasetName value object
            builder.Property(d => d.Name)
                .HasConversion(
                    v => v.Value,
                    v => DatasetName.Create(v))
                .HasMaxLength(50)
                .IsRequired();

            // Configure Description value object - allow NULL
            builder.Property(d => d.Description)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => v != null ? Description.Create(v) : Description.Create(null))
                .HasMaxLength(255)
                .IsRequired(false);

            // Audit fields
            builder.Property(d => d.CreatedAt)
                .IsRequired();

            builder.Property(d => d.CreatedBy)
                .HasMaxLength(100);

            builder.Property(d => d.ModifiedAt);

            builder.Property(d => d.ModifiedBy)
                .HasMaxLength(100);

            // Optimistic concurrency token
            builder.Property(d => d.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationships
            builder.HasMany(d => d.Images)
                .WithOne()
                .HasForeignKey(i => i.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Projects)
                .WithOne(p => p.Dataset)
                .HasForeignKey(p => p.DatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore domain events
            builder.Ignore(d => d.DomainEvents);

            // Computed property
            builder.Ignore(d => d.ImageCount);

            // Indexes
            builder.HasIndex(d => d.Name)
                .IsUnique();

            builder.HasIndex(d => d.CreatedAt);
        }
    }
}
