using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Datasets.Queries.GetDatasets;

/// <summary>
/// Handles retrieving paginated datasets with their counts
/// Efficiently loads image and project counts to prevent N+1 queries
/// </summary>
public class GetDatasetsQueryHandler : IQueryHandler<GetDatasetsQuery, Result<PagedResult<DatasetDto>>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDatasetsQueryHandler> _logger;

    public GetDatasetsQueryHandler(
        IDatasetRepository datasetRepository,
        IImageRepository imageRepository,
        IProjectRepository projectRepository,
        IMapper mapper,
        ILogger<GetDatasetsQueryHandler> logger)
    {
        _datasetRepository = datasetRepository;
        _imageRepository = imageRepository;
        _projectRepository = projectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PagedResult<DatasetDto>>> Handle(GetDatasetsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Fetching datasets with counts, Page {PageNumber}, PageSize {PageSize}",
                request.PageNumber, request.PageSize);

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result.Failure<PagedResult<DatasetDto>>("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result.Failure<PagedResult<DatasetDto>>("Page size must be between 1 and 100");
            }

            // Step 1: Fetch paginated datasets
            var (datasets, totalCount) = await _datasetRepository.GetAllPagedAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            if (!datasets.Any())
            {
                _logger.LogInformation("No datasets found for page {PageNumber}", request.PageNumber);
                var emptyResult = new PagedResult<DatasetDto>(
                    Array.Empty<DatasetDto>(),
                    totalCount,
                    request.PageNumber,
                    request.PageSize);
                return Result.Success(emptyResult);
            }

            // Step 2: Get dataset IDs for the current page only
            var datasetIds = datasets.Select(d => d.Id).ToList();

            // Step 3: Get image counts ONLY for datasets on current page (optimized)
            var imageCounts = await _imageRepository.GetCountsByDatasetIdsAsync(datasetIds, cancellationToken);

            // Step 4: Get project counts ONLY for datasets on current page (optimized)
            var projectCounts = await _projectRepository.GetCountsByDatasetIdsAsync(datasetIds, cancellationToken);

            // Step 5: Map datasets to DTOs and populate counts
            var datasetDtos = datasets.Select(dataset =>
            {
                var dto = _mapper.Map<DatasetDto>(dataset);
                dto.ImageCount = imageCounts.GetValueOrDefault(dataset.Id, 0);
                return dto;
            }).ToList();

            // Step 5: Create paged result
            var pagedResult = new PagedResult<DatasetDto>(
                datasetDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation(
                "Fetched {ItemCount} datasets out of {TotalCount}",
                datasetDtos.Count, totalCount);

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching datasets: {ErrorMessage}", ex.Message);
            return Result.Failure<PagedResult<DatasetDto>>($"Failed to fetch datasets: {ex.Message}");
        }
    }
}
