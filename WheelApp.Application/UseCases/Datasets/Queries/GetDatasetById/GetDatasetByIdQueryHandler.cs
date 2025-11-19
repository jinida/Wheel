using AutoMapper;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Datasets.Queries.GetDatasetById;

/// <summary>
/// Handles retrieving a single dataset by ID
/// </summary>
public class GetDatasetByIdQueryHandler : IQueryHandler<GetDatasetByIdQuery, Result<DatasetDto>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IMapper _mapper;

    public GetDatasetByIdQueryHandler(
        IDatasetRepository datasetRepository,
        IMapper mapper)
    {
        _datasetRepository = datasetRepository;
        _mapper = mapper;
    }

    public async Task<Result<DatasetDto>> Handle(GetDatasetByIdQuery request, CancellationToken cancellationToken)
    {
        // Fetch dataset by ID
        var dataset = await _datasetRepository.GetByIdAsync(request.Id, cancellationToken);

        if (dataset == null)
        {
            return Result.Failure<DatasetDto>($"Dataset with ID {request.Id} was not found.");
        }

        // Map to DTO and return
        var datasetDto = _mapper.Map<DatasetDto>(dataset);
        return Result.Success(datasetDto);
    }
}
