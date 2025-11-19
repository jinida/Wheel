using System.Text.Json.Serialization;

namespace WheelApp.Application.DTOs;

/// <summary>
/// DTO for importing annotations from JSON
/// </summary>
public class ImportAnnotationDto
{
    [JsonPropertyName("header")]
    public ImportHeaderDto? Header { get; set; }

    [JsonPropertyName("annotations")]
    public List<ImportAnnotationItemDto> Annotations { get; set; } = new();
}

/// <summary>
/// Import header metadata
/// </summary>
public class ImportHeaderDto
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Individual annotation item for import
/// </summary>
public class ImportAnnotationItemDto
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Label can be:
    /// - Integer/Number for classification/anomaly detection
    /// - Array for object detection/segmentation
    /// Using JsonElement to handle dynamic types
    /// </summary>
    [JsonPropertyName("label")]
    public System.Text.Json.JsonElement Label { get; set; }

    [JsonPropertyName("role")]
    public int Role { get; set; }
}

/// <summary>
/// Result of import operation
/// </summary>
public class ImportAnnotationResultDto
{
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> FailedItems { get; set; } = new();
    public List<string> Messages { get; set; } = new();
}