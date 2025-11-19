using WheelApp.Domain.Entities;

namespace WheelApp.Domain.Events.RoleEvents
{
    /// <summary>
    /// Raised when a role is assigned to an image
    /// </summary>
    public class RoleAssignedEvent : IDomainEvent
    {
        public Role Role { get; }
        public DateTime OccurredOn { get; }

        public RoleAssignedEvent(Role role)
        {
            Role = role;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
