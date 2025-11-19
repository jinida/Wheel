using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Annotations.Queries.ExportAnnotations;

/// <summary>
/// Query to export annotations for a project
/// </summary>
public class ExportAnnotationsQuery : IQuery<Result<ExportAnnotationDto>>
{
    /// <summary>
    /// The project ID to export annotations for
    /// </summary>
    public int ProjectId { get; set; }
}