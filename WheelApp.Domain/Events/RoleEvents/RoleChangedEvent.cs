using WheelApp.Domain.Entities;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Events.RoleEvents
{
    /// <summary>
    /// Raised when a role type is changed
    /// </summary>
    public class RoleChangedEvent : IDomainEvent
    {
        public Role Role { get; }
        public RoleType OldRoleType { get; }
        public RoleType NewRoleType { get; }
        public DateTime OccurredOn { get; }

        public RoleChangedEvent(Role role, RoleType oldRoleType, RoleType newRoleType)
        {
            Role = role;
            OldRoleType = oldRoleType;
            NewRoleType = newRoleType;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
