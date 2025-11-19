using AutoMapper;
using System.Text.Json;
using WheelApp.Application.DTOs;
using WheelApp.Domain.Entities;

namespace WheelApp.Application.Mappings;

/// <summary>
/// AutoMapper profile for Annotation entity mappings
/// Handles bidirectional conversion between JSON string (Entity) and List<Point2f> (DTO)
/// </summary>
public class AnnotationMappingProfile : Profile
{
    public AnnotationMappingProfile()
    {
        // Entity → DTO: Parse JSON string to List<Point2f> and include nested ProjectClassDto
        CreateMap<Annotation, AnnotationDto>()
            .ForMember(dest => dest.Information, opt => opt.MapFrom(src => ParseInformationToPoints(src.Information)))
            .ForMember(dest => dest.classDto, opt => opt.MapFrom(src => src.ProjectClass))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // DTO → Entity: Convert List<Point2f> to JSON string
        CreateMap<AnnotationDto, Annotation>()
            .ForMember(dest => dest.Information, opt => opt.MapFrom(src => ConvertPointsToJsonString(src.Information)))
            .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.classDto != null ? src.classDto.Id : 0))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }

    private static List<Point2f> ParseInformationToPoints(string? jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
            return new List<Point2f>();

        try
        {
            // First, try to parse as nested array [[x1, y1], [x2, y2], ...]
            try
            {
                var nestedCoords = JsonSerializer.Deserialize<float[][]>(jsonString);
                if (nestedCoords != null && nestedCoords.Length > 0)
                {
                    var points = new List<Point2f>();
                    foreach (var coord in nestedCoords)
                    {
                        if (coord != null && coord.Length >= 2)
                        {
                            points.Add(new Point2f(coord[0], coord[1]));
                        }
                    }
                    if (points.Count > 0)
                        return points;
                }
            }
            catch
            {
                // If nested array parsing fails, try flat array format
            }

            // Fallback: Try to parse as flat array [x1, y1, x2, y2, ...]
            var coords = JsonSerializer.Deserialize<float[]>(jsonString);
            if (coords == null || coords.Length == 0)
                return new List<Point2f>();

            // Convert flat array [x1, y1, x2, y2, ...] to List<Point2f>
            var flatPoints = new List<Point2f>();
            for (int i = 0; i < coords.Length; i += 2)
            {
                if (i + 1 < coords.Length)
                {
                    flatPoints.Add(new Point2f(coords[i], coords[i + 1]));
                }
            }
            return flatPoints;
        }
        catch
        {
            // Return empty list if JSON parsing fails
            return new List<Point2f>();
        }
    }

    /// <summary>
    /// Converts List<Point2f> from DTO to JSON string for Entity
    /// Format: List<Point2f> -> "[x1,y1,x2,y2,...,xn,yn]"
    /// Uses comma separator without spaces for compact storage
    /// </summary>
    private static string? ConvertPointsToJsonString(List<Point2f>? points)
    {
        if (points == null || points.Count == 0)
            return null;

        try
        {
            // Convert List<Point2f> to flat float array
            var coords = new List<float>();
            foreach (var point in points)
            {
                coords.Add(point.X);
                coords.Add(point.Y);
            }

            // Serialize to JSON with minimal formatting (no spaces)
            var options = new JsonSerializerOptions
            {
                WriteIndented = false  // Compact format: [1,2,3,4] not [ 1, 2, 3, 4 ]
            };

            return JsonSerializer.Serialize(coords.ToArray(), options);
        }
        catch
        {
            return null;
        }
    }
}
