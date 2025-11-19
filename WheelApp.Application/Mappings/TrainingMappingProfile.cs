using AutoMapper;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;

namespace WheelApp.Application.Mappings;

/// <summary>
/// AutoMapper profile for Training entity mappings
/// Note: Progress is not mapped here - it's calculated dynamically by Infrastructure layer
/// </summary>
public class TrainingMappingProfile : Profile
{
    public TrainingMappingProfile()
    {
        CreateMap<Training, TrainingDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Value))
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.Name.Value : ""))
            .ForMember(dest => dest.DatasetName, opt => opt.MapFrom(src => src.Project != null && src.Project.Dataset != null ? src.Project.Dataset.Name.Value : ""))
            .ForMember(dest => dest.TaskType, opt => opt.MapFrom(src => src.Project != null && src.Project.Type != null ? src.Project.Type.Name : ""))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.Value))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.Name))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.EndedAt, opt => opt.MapFrom(src => src.EndedAt))
            .ForMember(dest => dest.Progress, opt => opt.Ignore()); // Progress calculated separately
    }
}
