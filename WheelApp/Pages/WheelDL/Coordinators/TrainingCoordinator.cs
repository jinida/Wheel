using MediatR;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Trainings.Commands.CreateTraining;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for training operations
    /// </summary>
    public class TrainingCoordinator : BaseProjectCoordinator
    {
        public TrainingCoordinator(
            IMediator mediator,
            ProjectWorkspaceService workspaceService)
            : base(mediator, workspaceService)
        {
        }

        /// <summary>
        /// Creates a new training with Pending status
        /// </summary>
        public async Task<Result<TrainingDto>> CreateTrainingAsync(int projectId, string trainingName)
        {
            var command = new CreateTrainingCommand
            {
                ProjectId = projectId,
                Name = trainingName
            };

            return await _mediator.Send(command);
        }
    }
}
