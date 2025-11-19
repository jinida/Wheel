namespace WheelApp.Application.DTOs;

/// <summary>
/// Project data transfer object
/// </summary>
public class ProjectDto
{
    public int Id { get; set; }
    public int DatasetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
