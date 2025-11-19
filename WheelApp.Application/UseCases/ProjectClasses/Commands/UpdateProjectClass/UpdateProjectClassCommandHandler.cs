using AutoMapper;
using WheelApp.Application.Common.Exceptions;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Repositories;

namespace WheelApp.Application.UseCases.ProjectClasses.Commands.UpdateProjectClass;

/// <summary>
/// Handles updating a project class
/// All validation is performed by FluentValidation in the pipeline
/// </summary>
public class UpdateProjectClassCommandHandler : ICommandHandler<UpdateProjectClassCommand, Result<ProjectClassDto>>
{
    private readonly IProjectClassRepository _projectClassRepository;
    private readonly IMapper _mapper;

    public UpdateProjectClassCommandHandler(
        IProjectClassRepository projectClassRepository,
        IMapper mapper)
    {
        _projectClassRepository = projectClassRepository;        _mapper = mapper;
    }

    public async Task<Result<ProjectClassDto>> Handle(UpdateProjectClassCommand request, CancellationToken cancellationToken)
    {
        // All validation is handled by FluentValidation in the pipeline
        // Fetch the project class (guaranteed to exist by validator)
        var projectClass = await _projectClassRepository.GetByIdAsync(request.Id, cancellationToken);

        // Update using domain methods
        projectClass!.UpdateName(request.Name);
        projectClass.UpdateColor(request.Color);

        // Persist changes
        await _projectClassRepository.UpdateAsync(projectClass, cancellationToken);
        // Changes are saved automatically by TransactionBehavior

        // Map to DTO and return
        var projectClassDto = _mapper.Map<ProjectClassDto>(projectClass);
        return Result.Success(projectClassDto);
    }
}
