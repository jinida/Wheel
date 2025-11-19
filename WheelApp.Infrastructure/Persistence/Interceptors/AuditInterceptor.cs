using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WheelApp.Application.Common.Interfaces;

namespace WheelApp.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Interceptor for automatically setting audit fields on entities
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;

        public AuditInterceptor(ICurrentUserService currentUserService, IDateTime dateTime)
        {
            _currentUserService = currentUserService;
            _dateTime = dateTime;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                UpdateAuditFields(eventData.Context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateAuditFields(DbContext context)
        {
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            var currentUserId = _currentUserService.UserId ?? "System";
            var currentTime = _dateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    // Set CreatedAt and CreatedBy for new entities only if not already set
                    SetPropertyIfNotSet(entry.Entity, "CreatedAt", currentTime);
                    SetPropertyIfNotSet(entry.Entity, "CreatedBy", currentUserId);
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Set ModifiedAt and ModifiedBy for modified entities
                    SetPropertyIfExists(entry.Entity, "ModifiedAt", currentTime);
                    SetPropertyIfExists(entry.Entity, "ModifiedBy", currentUserId);
                }
            }
        }

        private static void SetPropertyIfExists(object entity, string propertyName, object value)
        {
            var property = entity.GetType().GetProperty(propertyName);

            if (property is not null && property.CanWrite)
            {
                // Check if the property type matches the value type
                if (property.PropertyType.IsAssignableFrom(value.GetType()) ||
                    (property.PropertyType.IsGenericType &&
                     property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                     Nullable.GetUnderlyingType(property.PropertyType) == value.GetType()))
                {
                    property.SetValue(entity, value);
                }
            }
        }

        private static void SetPropertyIfNotSet(object entity, string propertyName, object value)
        {
            var property = entity.GetType().GetProperty(propertyName);

            if (property is not null && property.CanWrite && property.CanRead)
            {
                // Get the current value
                var currentValue = property.GetValue(entity);

                // Only set if the current value is default (null, DateTime.MinValue, etc.)
                bool isDefaultValue = currentValue == null ||
                                      (currentValue is DateTime dt && dt == DateTime.MinValue) ||
                                      (currentValue is string str && string.IsNullOrEmpty(str));

                if (isDefaultValue)
                {
                    // Check if the property type matches the value type
                    if (property.PropertyType.IsAssignableFrom(value.GetType()) ||
                        (property.PropertyType.IsGenericType &&
                         property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                         Nullable.GetUnderlyingType(property.PropertyType) == value.GetType()))
                    {
                        property.SetValue(entity, value);
                    }
                }
            }
        }
    }
}
