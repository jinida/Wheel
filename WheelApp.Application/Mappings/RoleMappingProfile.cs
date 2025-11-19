using AutoMapper;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;

namespace WheelApp.Application.Mappings;

/// <summary>
/// AutoMapper profile for Role entity mappings
/// </summary>
public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => GetRoleTypeName(src.RoleType)))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ImageId, opt => opt.MapFrom(src => src.ImageId));
    }

    private static string GetRoleTypeName(int roleType)
    {
        return roleType switch
        {
            0 => "Train",
            1 => "Validation",
            2 => "Test",
            3 => "None",
            _ => "Unknown"
        };
    }
}
