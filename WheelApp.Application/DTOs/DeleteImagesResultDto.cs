namespace WheelApp.Application.DTOs;

/// <summary>
/// Result DTO for image deletion operations
/// </summary>
public class DeleteImagesResultDto
{
    public int DeletedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> DeletedNames { get; set; } = new();
    public List<string> FailedNames { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
