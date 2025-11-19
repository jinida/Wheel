using WheelApp.Domain.Common;
using WheelApp.Domain.Events.RoleEvents;
using WheelApp.Domain.Exceptions;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Domain.Entities
{
    /// <summary>
    /// Role entity for assigning images to training splits
    /// </summary>
    public class Role : Entity
    {
        public int ImageId { get; private set; }
        public int ProjectId { get; private set; }
        public RoleType RoleType { get; private set; }

        private Role() { }  // For EF Core

        private Role(int imageId, int projectId, RoleType roleType)
        {
            ImageId = imageId;
            ProjectId = projectId;
            RoleType = roleType;
        }

        /// <summary>
        /// Factory method to create a new role for an image
        /// </summary>
        public static Role Create(int imageId, int projectId, int roleTypeValue)
        {
            if (imageId <= 0)
                throw new ValidationException(nameof(imageId), "Image ID must be positive.");

            if (projectId <= 0)
                throw new ValidationException(nameof(projectId), "Project ID must be positive.");

            var roleType = RoleType.FromValue(roleTypeValue);
            var role = new Role(imageId, projectId, roleType);
            role.AddDomainEvent(new RoleAssignedEvent(role));
            return role;
        }

        /// <summary>
        /// Changes the role type
        /// </summary>
        public void ChangeRole(int newRoleTypeValue)
        {
            var oldRoleType = RoleType;
            var newRoleType = RoleType.FromValue(newRoleTypeValue);
            RoleType = newRoleType;
            AddDomainEvent(new RoleChangedEvent(this, oldRoleType, newRoleType));
        }
    }
}
