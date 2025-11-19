using AutoMapper;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;

namespace WheelApp.Application.Mappings;

/// <summary>
/// AutoMapper profile for Dataset entity mappings
/// </summary>
public class DatasetMappingProfile : Profile
{
    public DatasetMappingProfile()
    {
        CreateMap<Dataset, DatasetDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Value))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description != null ? src.Description.Value : null))
            .ForMember(dest => dest.ImageCount, opt => opt.MapFrom(src => src.Images.Count))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy ?? "Unknown"))
            .ForMember(dest => dest.ModifiedBy, opt => opt.MapFrom(src => src.ModifiedBy))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt));
    }
}
