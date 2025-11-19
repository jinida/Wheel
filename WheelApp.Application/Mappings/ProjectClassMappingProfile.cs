using AutoMapper;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;

namespace WheelApp.Application.Mappings;

/// <summary>
/// AutoMapper profile for ProjectClass entity mappings
/// </summary>
public class ProjectClassMappingProfile : Profile
{
    public ProjectClassMappingProfile()
    {
        CreateMap<ProjectClass, ProjectClassDto>()
            .ForMember(dest => dest.ClassIdx, opt => opt.MapFrom(src => src.ClassIdx.Value))
            .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color.Value));
    }
}
