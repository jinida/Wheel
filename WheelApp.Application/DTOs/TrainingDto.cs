namespace WheelApp.Application.DTOs;

/// <summary>
/// Training data transfer object
/// Progress is calculated dynamically, not stored in DB
/// </summary>
public class TrainingDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DatasetName { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int Progress { get; set; } // Calculated dynamically based on time elapsed
}
