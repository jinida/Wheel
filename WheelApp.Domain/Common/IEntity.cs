namespace WheelApp.Domain.Common
{
    /// <summary>
    /// Marks classes as entities with identity
    /// </summary>
    public interface IEntity
    {
        int Id { get; }
    }
}
