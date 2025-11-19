namespace WheelApp.Domain.Common
{
    /// <summary>
    /// Base class for entities requiring audit trails
    /// </summary>
    public abstract class AuditableEntity : Entity, IAuditable
    {
        public DateTime CreatedAt { get; protected set; }
        public string? CreatedBy { get; protected set; }
        public DateTime? ModifiedAt { get; protected set; }
        public string? ModifiedBy { get; protected set; }

        /// <summary>
        /// Updates the audit information with current modification details
        /// Note: ModifiedAt and ModifiedBy are also set automatically by AuditInterceptor
        /// This method is provided for explicit updates when needed
        /// </summary>
        public void UpdateAudit(string modifiedBy)
        {
            ModifiedAt = DateTime.UtcNow;
            ModifiedBy = modifiedBy;
        }
    }
}
