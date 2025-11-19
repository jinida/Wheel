using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.ValueObjects;

namespace WheelApp.Application.UseCases.ProjectTypes.Queries.GetProjectTypes;

/// <summary>
/// Handler for GetProjectTypesQuery
/// Retrieves all available project types from Domain layer and converts to DTOs
/// </summary>
public class GetProjectTypesQueryHandler : IQueryHandler<GetProjectTypesQuery, Result<List<ProjectTypeDto>>>
{
    public Task<Result<List<ProjectTypeDto>>> Handle(GetProjectTypesQuery request, CancellationToken cancellationToken)
    {
        // Get all project types from Domain layer
        var projectTypes = ProjectType.GetAll()
            .Select(pt => new ProjectTypeDto
            {
                Value = pt.Value,
                Name = pt.Name
            })
            .ToList();

        return Task.FromResult(Result.Success(projectTypes));
    }
}
