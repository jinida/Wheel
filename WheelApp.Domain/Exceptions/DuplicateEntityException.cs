namespace WheelApp.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to create a duplicate entity
    /// </summary>
    public class DuplicateEntityException : DomainException
    {
        public string EntityType { get; }
        public string DuplicateKey { get; }

        public DuplicateEntityException(string entityType, string duplicateKey)
            : base($"{entityType} with '{duplicateKey}' already exists.")
        {
            EntityType = entityType;
            DuplicateKey = duplicateKey;
        }

        public DuplicateEntityException(string entityType, string duplicateKey, string message)
            : base(message)
        {
            EntityType = entityType;
            DuplicateKey = duplicateKey;
        }
    }
}
