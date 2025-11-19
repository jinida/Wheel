using System.Runtime.CompilerServices;
using WheelApp.Domain.Events;

namespace WheelApp.Domain.Common
{
    /// <summary>
    /// Base class for all entities with identity
    /// </summary>
    public abstract class Entity : IEntity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public int Id { get; protected set; }
        public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Adds a domain event to the entity's event collection
        /// </summary>
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Clears all domain events from the entity
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// Equality based on Id
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not Entity other)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (Id == 0 || other.Id == 0)
                return ReferenceEquals(this, other);  // Use reference equality for new entities

            return Id == other.Id;
        }

        /// <summary>
        /// Hash code based on reference to ensure stability across entity lifecycle
        /// Using reference-based hash code prevents issues when entities are added to
        /// hash-based collections before being persisted (when Id changes from 0 to actual value)
        /// </summary>
        public override int GetHashCode()
        {
            // Always use reference-based hash code for stability
            // This ensures hash code never changes, even when entity is saved and Id is assigned
            return RuntimeHelpers.GetHashCode(this);
        }

        public static bool operator ==(Entity? a, Entity? b)
        {
            if (a is null && b is null)
                return true;

            if (a is null || b is null)
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(Entity? a, Entity? b)
        {
            return !(a == b);
        }
    }
}
