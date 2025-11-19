namespace WheelApp.Application.DTOs;

public class ImageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public List<AnnotationDto> Annotation { get; set; } = new();
    public RoleTypeDto? RoleType { get; set; }
}