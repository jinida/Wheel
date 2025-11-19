namespace WheelApp.Domain.Common
{
    /// <summary>
    /// Contract for auditable entities
    /// </summary>
    public interface IAuditable
    {
        DateTime CreatedAt { get; }
        string? CreatedBy { get; }
        DateTime? ModifiedAt { get; }
        string? ModifiedBy { get; }
    }
}
