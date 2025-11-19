using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Training entity
    /// </summary>
    public class TrainingConfiguration : IEntityTypeConfiguration<Training>
    {
        public void Configure(EntityTypeBuilder<Training> builder)
        {
            builder.ToTable("Training");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .ValueGeneratedOnAdd();

            builder.Property(t => t.ProjectId)
                .IsRequired();

            // Configure TrainingName value object
            builder.Property(t => t.Name)
                .HasConversion(
                    v => v.Value,
                    v => WheelApp.Domain.ValueObjects.TrainingName.Create(v))
                .HasMaxLength(200)
                .IsRequired();

            // Configure TrainingStatus value object
            builder.Property(t => t.Status)
                .HasConversion(
                    v => v.Value,
                    v => TrainingStatus.FromValue(v))
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(t => t.EndedAt);

            // Optimistic concurrency token
            builder.Property(t => t.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationships
            builder.HasOne(t => t.Project)
                .WithMany(p => p.Trainings)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Evaluations)
                .WithOne()
                .HasForeignKey(e => e.TrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(t => t.DomainEvents);

            // Indexes
            builder.HasIndex(t => t.ProjectId);

            builder.HasIndex(t => t.Status);

            builder.HasIndex(t => t.CreatedAt);

            // Composite index for project and status
            builder.HasIndex(t => new { t.ProjectId, t.Status });
        }
    }
}
