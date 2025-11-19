using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.Common.Models;
using WheelApp.Application.DTOs;

namespace WheelApp.Application.UseCases.Images.Commands.DeleteImages;

/// <summary>
/// Command to delete multiple images from a dataset
/// </summary>
public record DeleteImagesCommand : ICommand<Result<DeleteImagesResultDto>>
{
    public List<int> ImageIds { get; init; } = new();
}
