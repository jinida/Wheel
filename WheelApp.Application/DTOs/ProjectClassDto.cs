namespace WheelApp.Application.DTOs;

/// <summary>
/// Project class data transfer object
/// </summary>
public class ProjectClassDto
{
    public int Id { get; set; }
    public int ClassIdx { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
