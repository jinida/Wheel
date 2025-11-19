namespace WheelApp.Application.DTOs;

/// <summary>
/// DTO for project type information
/// </summary>
public class ProjectTypeDto
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if this project type supports multiple labels per image
    /// (ObjectDetection and Segmentation support multiple labels)
    /// </summary>
    public bool MultiLabelType => Value == 1 || Value == 2; // ObjectDetection or Segmentation
}
