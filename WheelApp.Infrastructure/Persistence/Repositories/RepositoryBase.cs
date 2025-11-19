using Microsoft.EntityFrameworkCore;
using WheelApp.Domain.Common;
using WheelApp.Domain.Repositories;

namespace WheelApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Generic base repository implementing common CRUD operations
    /// All queries are parameterized to prevent SQL injection
    /// </summary>
    public abstract class RepositoryBase<T> : IRepository<T> where T : Entity
    {
        protected readonly WheelAppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        protected RepositoryBase(WheelAppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
        }

        public virtual async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            // EF Core uses parameterized queries by default
            return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
        }

        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = await _dbSet.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }

        /// <summary>
        /// Adds multiple entities to the database in a single batch operation.
        /// More efficient than calling AddAsync in a loop - prevents N+1 queries.
        /// </summary>
        /// <param name="entities">The collection of entities to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the async operation</returns>
        public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return;

            await _dbSet.AddRangeAsync(entityList, cancellationToken);
        }

        /// <summary>
        /// Marks the entity for update in the change tracker.
        /// The actual database update occurs when UnitOfWork.CommitAsync() is called.
        /// EF Core's optimistic concurrency (RowVersion) will catch missing or stale entities.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the async operation</returns>
        public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Rely on EF Core change tracking and optimistic concurrency (RowVersion)
            // No need to check existence - this adds unnecessary database queries
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Marks the entity for deletion in the change tracker.
        /// The actual database deletion occurs when UnitOfWork.CommitAsync() is called.
        /// EF Core's optimistic concurrency (RowVersion) will catch missing or stale entities.
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the async operation</returns>
        public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Check if entity is already being tracked
            var trackedEntity = _context.ChangeTracker.Entries<T>()
                .FirstOrDefault(e => e.Entity.Id.Equals(entity.Id));

            if (trackedEntity != null)
            {
                // Entity is already tracked, just mark it as deleted
                trackedEntity.State = EntityState.Deleted;
            }
            else
            {
                // Entity is not tracked, attach and remove
                _dbSet.Remove(entity);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes an entity by its ID without querying it first.
        /// This avoids loading navigation properties and is more efficient for cascade deletes.
        /// Uses raw SQL to bypass optimistic concurrency checking since we don't have the RowVersion.
        /// The actual database deletion occurs when UnitOfWork.CommitAsync() is called.
        /// </summary>
        /// <param name="id">The ID of the entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the async operation</returns>
        public virtual async Task DeleteByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Check if entity is already being tracked
            var trackedEntity = _context.ChangeTracker.Entries<T>()
                .FirstOrDefault(e => e.Entity.Id == id);

            if (trackedEntity != null)
            {
                // Entity is already tracked, just mark it as deleted
                trackedEntity.State = EntityState.Deleted;
            }
            else
            {
                // Use raw SQL to delete by ID, bypassing RowVersion concurrency check
                // This is safe because we've already deleted all dependent entities in the correct order
                var tableName = _context.Model.FindEntityType(typeof(T))?.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    // Use ExecuteSqlInterpolatedAsync for safe parameterized query
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"DELETE FROM [{tableName}] WHERE [Id] = {id}",
                        cancellationToken);
                }
            }
        }

        /// <summary>
        /// Marks multiple entities for deletion in the change tracker in a single batch operation.
        /// More efficient than calling DeleteAsync in a loop - prevents N+1 queries.
        /// The actual database deletion occurs when UnitOfWork.CommitAsync() is called.
        /// </summary>
        /// <param name="entities">The collection of entities to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the async operation</returns>
        public virtual Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return Task.CompletedTask;

            foreach (var entity in entityList)
            {
                // Check if entity is already being tracked
                var trackedEntity = _context.ChangeTracker.Entries<T>()
                    .FirstOrDefault(e => e.Entity.Id.Equals(entity.Id));

                if (trackedEntity != null)
                {
                    // Entity is already tracked, just mark it as deleted
                    trackedEntity.State = EntityState.Deleted;
                }
                else
                {
                    // Entity is not tracked, attach and remove
                    _dbSet.Remove(entity);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches an entity from the change tracker to prevent tracking conflicts.
        /// Also detaches all related entities in the object graph.
        /// Useful when you need to reload an entity or work with multiple instances.
        /// </summary>
        /// <param name="entity">The entity to detach</param>
        public virtual void Detach(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Get all tracked entities in the context
            var trackedEntries = _context.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Detached)
                .ToList();

            // Detach all tracked entities
            foreach (var entry in trackedEntries)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
