using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Evaluation entity
    /// </summary>
    public class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
    {
        public void Configure(EntityTypeBuilder<Evaluation> builder)
        {
            builder.ToTable("Evaluation");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            builder.Property(e => e.TrainingId)
                .IsRequired();

            // Configure FilePath value object
            builder.Property(e => e.Path)
                .HasConversion(
                    v => v.Value,
                    v => FilePath.Create(v))
                .HasMaxLength(512)
                .IsRequired();

            // MetricsJson stores evaluation metrics as JSON
            builder.Property(e => e.MetricsJson)
                .HasMaxLength(4000);

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            // Optimistic concurrency token
            builder.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationship with Training
            builder.HasOne<Training>()
                .WithMany(t => t.Evaluations)
                .HasForeignKey(e => e.TrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(e => e.DomainEvents);

            // Indexes
            builder.HasIndex(e => e.TrainingId);

            builder.HasIndex(e => e.CreatedAt);
        }
    }
}
