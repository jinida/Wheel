using Microsoft.EntityFrameworkCore;

using WheelApp.Domain.Common;

using WheelApp.Domain.Entities;

using WheelApp.Infrastructure.Persistence.Configurations;



namespace WheelApp.Infrastructure.Persistence

{

    /// <summary>

    /// EF Core DbContext for WheelApp

    /// </summary>

    public class WheelAppDbContext : DbContext

    {

        public WheelAppDbContext(DbContextOptions<WheelAppDbContext> options)

            : base(options)

        {

        }



        public DbSet<Dataset> Datasets => Set<Dataset>();

        public DbSet<Project> Projects => Set<Project>();

        public DbSet<Image> Images => Set<Image>();

        public DbSet<Annotation> Annotations => Set<Annotation>();

        public DbSet<ProjectClass> ProjectClasses => Set<ProjectClass>();

        public DbSet<Training> Trainings => Set<Training>();

        public DbSet<Evaluation> Evaluations => Set<Evaluation>();

        public DbSet<Role> Roles => Set<Role>();



        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {

            base.OnModelCreating(modelBuilder);



            // Configure optimistic concurrency for all entities

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()

                .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType)))

            {

                modelBuilder.Entity(entityType.ClrType)

                    .Property<byte[]>("RowVersion")

                    .IsRowVersion()

                    .IsConcurrencyToken();

            }



            // Apply all entity configurations

            modelBuilder.ApplyConfiguration(new DatasetConfiguration());

            modelBuilder.ApplyConfiguration(new ProjectConfiguration());

            modelBuilder.ApplyConfiguration(new ImageConfiguration());

            modelBuilder.ApplyConfiguration(new AnnotationConfiguration());

            modelBuilder.ApplyConfiguration(new ProjectClassConfiguration());

            modelBuilder.ApplyConfiguration(new TrainingConfiguration());

            modelBuilder.ApplyConfiguration(new EvaluationConfiguration());

            modelBuilder.ApplyConfiguration(new RoleConfiguration());

        }



        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)

        {

            // Handle audit fields for auditable entities

            var entries = ChangeTracker.Entries<AuditableEntity>();



            foreach (var entry in entries)

            {

                if (entry.State == EntityState.Added)

                {

                    // CreatedAt and CreatedBy are set in the domain entity

                    // No need to override here

                }

                else if (entry.State == EntityState.Modified)

                {

                    // ModifiedAt and ModifiedBy are set via UpdateAudit method in domain

                    // No need to override here

                }

            }



            // Save changes

            // Note: Domain events are cleared by UnitOfWork after dispatching

            return await base.SaveChangesAsync(cancellationToken);

        }

    }

}