using AutoMapper;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;

namespace WheelApp.Application.Mappings;

/// <summary>
/// AutoMapper profile for Image entity mappings
/// </summary>
public class ImageMappingProfile : Profile
{
    public ImageMappingProfile()
    {
        CreateMap<Image, ImageDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Path.Value))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.Annotation, opt => opt.Ignore())  // Manually mapped in handler with nested classes
            .ForMember(dest => dest.RoleType, opt => opt.Ignore());    // Manually mapped in handler from Role entity
    }
}
