using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Projects.Queries.GetProjectsByDataset;

/// <summary>
/// Handles retrieving paginated projects for a specific dataset
/// </summary>
public class GetProjectsByDatasetQueryHandler : IQueryHandler<GetProjectsByDatasetQuery, Result<PagedResult<ProjectDto>>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProjectsByDatasetQueryHandler> _logger;

    public GetProjectsByDatasetQueryHandler(
        IProjectRepository projectRepository,
        IMapper mapper,
        ILogger<GetProjectsByDatasetQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ProjectDto>>> Handle(GetProjectsByDatasetQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Fetching projects for Dataset {DatasetId}, Page {PageNumber}, PageSize {PageSize}",
                request.DatasetId, request.PageNumber, request.PageSize);

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result.Failure<PagedResult<ProjectDto>>("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result.Failure<PagedResult<ProjectDto>>("Page size must be between 1 and 100");
            }

            // Get paginated projects
            var (projects, totalCount) = await _projectRepository.GetByDatasetIdPagedAsync(
                request.DatasetId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Map to DTOs
            var projectDtos = _mapper.Map<IReadOnlyList<ProjectDto>>(projects);

            // Create paged result
            var pagedResult = new PagedResult<ProjectDto>(
                projectDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation(
                "Fetched {ItemCount} projects out of {TotalCount} for Dataset {DatasetId}",
                projectDtos.Count, totalCount, request.DatasetId);

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching projects for Dataset {DatasetId}: {ErrorMessage}",
                request.DatasetId, ex.Message);
            return Result.Failure<PagedResult<ProjectDto>>($"Failed to fetch projects: {ex.Message}");
        }
    }
}
