namespace WheelApp.Application.DTOs;

/// <summary>
/// Annotation data transfer object
/// Provides strongly-typed geometry data and computed properties
/// </summary>
public class AnnotationDto
{
    public int Id { get; set; }
    public int imageId { get; set; }
    public List<Point2f> Information { get; set; } = new List<Point2f>();
    public ProjectClassDto classDto { get; set; }
    public DateTime CreatedAt { get; set; }
}
