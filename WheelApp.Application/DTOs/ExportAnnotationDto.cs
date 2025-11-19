namespace WheelApp.Application.DTOs;

/// <summary>
/// DTO for exporting annotations in JSON format
/// </summary>
public class ExportAnnotationDto
{
    /// <summary>
    /// Export header containing metadata
    /// </summary>
    public ExportHeaderDto Header { get; set; } = new();

    /// <summary>
    /// List of annotations
    /// </summary>
    public List<ExportAnnotationItemDto> Annotations { get; set; } = new();
}

/// <summary>
/// Export header metadata
/// </summary>
public class ExportHeaderDto
{
    public string Version { get; set; } = "1.0.0";
    public string Type { get; set; } = "unknown";
    public string Creator { get; set; } = "WheelApp";
    public List<string> Categories { get; set; } = new();
    public string Description { get; set; } = "-";
}

/// <summary>
/// Individual annotation item for export
/// </summary>
public class ExportAnnotationItemDto
{
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Label can be:
    /// - Integer for classification/anomaly detection (single class index)
    /// - Array of arrays for object detection/segmentation (multiple annotations with coordinates)
    /// </summary>
    public object Label { get; set; } = 0;

    /// <summary>
    /// Role type: 0=Unknown, 1=Training, 2=Validation, 3=Test
    /// </summary>
    public int Role { get; set; }
}