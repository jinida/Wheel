using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Role entity
    /// </summary>
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            builder.Property(r => r.ImageId)
                .IsRequired();

            builder.Property(r => r.ProjectId)
                .IsRequired();

            // Configure RoleType value object
            builder.Property(r => r.RoleType)
                .HasConversion(
                    v => v.Value,
                    v => RoleType.FromValue(v))
                .IsRequired();

            // Optimistic concurrency token
            builder.Property(r => r.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Configure relationships - no explicit navigation properties in domain

            // Ignore domain events
            builder.Ignore(r => r.DomainEvents);

            // Indexes
            builder.HasIndex(r => r.ImageId);

            builder.HasIndex(r => r.ProjectId);

            builder.HasIndex(r => r.RoleType);

            // Composite indexes
            builder.HasIndex(r => new { r.ProjectId, r.RoleType });

            // Unique constraint on ImageId and ProjectId (each image can have only one role per project)
            builder.HasIndex(r => new { r.ImageId, r.ProjectId })
                .IsUnique();
        }
    }
}
