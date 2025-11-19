using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.ProjectTypes.Queries.GetProjectTypes;

/// <summary>
/// Query to get all available project types
/// </summary>
public record GetProjectTypesQuery : IQuery<Result<List<ProjectTypeDto>>>;
