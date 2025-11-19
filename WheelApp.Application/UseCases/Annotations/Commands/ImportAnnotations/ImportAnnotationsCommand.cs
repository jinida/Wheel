using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Commands.ImportAnnotations;

/// <summary>
/// Command to import annotations from JSON
/// </summary>
public class ImportAnnotationsCommand : ICommand<Result<ImportAnnotationResultDto>>
{
    /// <summary>
    /// The project ID to import annotations for
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// The JSON content containing annotations
    /// </summary>
    public string JsonContent { get; set; } = string.Empty;
}