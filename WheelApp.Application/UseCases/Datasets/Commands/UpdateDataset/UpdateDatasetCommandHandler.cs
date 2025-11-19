using AutoMapper;
using WheelApp.Application.Common.Exceptions;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.Datasets.Commands.UpdateDataset;

/// <summary>
/// Handles the update of an existing dataset
/// </summary>
public class UpdateDatasetCommandHandler : ICommandHandler<UpdateDatasetCommand, Result<DatasetDto>>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IMapper _mapper;

    public UpdateDatasetCommandHandler(
        IDatasetRepository datasetRepository,
        IMapper mapper)
    {
        _datasetRepository = datasetRepository;        _mapper = mapper;
    }

    public async Task<Result<DatasetDto>> Handle(UpdateDatasetCommand request, CancellationToken cancellationToken)
    {
        // Fetch the existing dataset
        var dataset = await _datasetRepository.GetByIdAsync(request.Id, cancellationToken);
        if (dataset == null)
        {
            throw new NotFoundException(nameof(Dataset), request.Id);
        }

        // Check if another dataset with the same name exists (excluding current dataset)
        var existingDataset = await _datasetRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existingDataset != null && existingDataset.Id != request.Id)
        {
            return Result.Failure<DatasetDto>($"Dataset with name '{request.Name}' already exists.");
        }

        // Update domain entity
        dataset.UpdateName(request.Name, request.ModifiedBy);
        dataset.UpdateDescription(request.Description, request.ModifiedBy);

        // Persist changes
        // Changes are saved automatically by TransactionBehavior

        // Map to DTO and return
        var datasetDto = _mapper.Map<DatasetDto>(dataset);
        return Result.Success(datasetDto);
    }
}
