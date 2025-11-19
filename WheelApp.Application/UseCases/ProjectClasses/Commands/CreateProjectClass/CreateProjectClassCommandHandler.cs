using AutoMapper;
using Microsoft.Extensions.Logging;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;
using WheelApp.Domain.Repositories;
using WheelApp.Domain.Specifications.ProjectClassSpecifications;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.CreateProjectClass;

/// <summary>
/// Handles the creation of a new project class with automatic ClassIdx assignment
/// Ensures zero-based sequential assignment within transaction for concurrency safety
/// </summary>
public class CreateProjectClassCommandHandler : ICommandHandler<CreateProjectClassCommand, Result<List<ProjectClassDto>>>
{
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProjectClassCommandHandler> _logger;

    public CreateProjectClassCommandHandler(
        IProjectClassRepository projectClassRepository,
        IMapper mapper,
        ILogger<CreateProjectClassCommandHandler> logger)
    {
        _projectClassRepository = projectClassRepository;        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<ProjectClassDto>>> Handle(CreateProjectClassCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // CRITICAL: Execute within transaction to prevent concurrency conflicts
            // This ensures accurate count at the moment of assignment

            // Get all existing classes for the project to determine next ClassIdx
            var existingClasses = await _projectClassRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

            // Determine the next ClassIdx based on domain rules:
            // 1. Zero-based sequential assignment
            // 2. New index = current count of existing classes
            int nextClassIdx = existingClasses.Count();

            _logger.LogInformation(
                "Assigning ClassIdx {ClassIdx} to new class '{ClassName}' in Project {ProjectId} (current count: {Count})",
                nextClassIdx, request.Name, request.ProjectId, existingClasses.Count());

            // Validate sequential integrity (ensure no gaps)
            var existingIndices = existingClasses
                .Where(c => c.ClassIdx != null)
                .Select(c => c.ClassIdx.Value)
                .OrderBy(idx => idx)
                .ToList();

            for (int i = 0; i < existingIndices.Count; i++)
            {
                if (existingIndices[i] != i)
                {
                    _logger.LogWarning(
                        "Non-sequential ClassIdx detected in Project {ProjectId}. Expected {Expected}, found {Found}",
                        request.ProjectId, i, existingIndices[i]);

                    // Could optionally fix the sequence here or return an error
                    return Result.Failure<List<ProjectClassDto>>(
                        $"Project class indices are not sequential. Please contact support to fix the data integrity issue.");
                }
            }

            // Create domain entity with auto-assigned ClassIdx
            var projectClass = ProjectClass.Create(
                request.ProjectId,
                nextClassIdx,
                request.Name,
                request.Color);

            // Persist to repository (transaction is managed by TransactionBehavior)
            await _projectClassRepository.AddAsync(projectClass, cancellationToken);

            _logger.LogInformation(
                "Successfully created ProjectClass with ID {ClassId} and ClassIdx {ClassIdx}",
                projectClass.Id, projectClass.ClassIdx?.Value);

            // Build the result list by adding the new class to existing classes
            // Note: We cannot query the DB again here because the transaction hasn't been committed yet
            var allClasses = existingClasses.ToList();
            allClasses.Add(projectClass);
            var allClassDtos = _mapper.Map<List<ProjectClassDto>>(allClasses.OrderBy(c => c.ClassIdx.Value).ToList());

            _logger.LogInformation(
                "Returning {Count} project classes including newly created class",
                allClassDtos.Count);

            return Result.Success(allClassDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create ProjectClass for Project {ProjectId}",
                request.ProjectId);
            return Result.Failure<List<ProjectClassDto>>($"Failed to create class: {ex.Message}");
        }
    }
}
