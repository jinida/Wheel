namespace WheelApp.Application.DTOs;

/// <summary>
/// Result DTO for annotation import operations
/// </summary>
public class ImportResultDto
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> ImportedItems { get; set; } = new();
    public List<string> SkippedItems { get; set; } = new();
    public List<string> FailedItems { get; set; } = new();
    public List<string> ErrorMessages { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
