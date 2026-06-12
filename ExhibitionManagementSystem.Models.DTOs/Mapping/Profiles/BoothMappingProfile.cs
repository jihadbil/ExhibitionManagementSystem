using System;
using System.Linq;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Booth;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class BoothMappingProfile : Profile
{
    public BoothMappingProfile()
    {
        CreateMap<Models.Booth, BoothDto>()
            .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => src.Hall != null ? src.Hall.HallName : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ShapeType, opt => opt.MapFrom(src => src.ShapeType.HasValue ? src.ShapeType.Value.ToString() : null));

        CreateMap<Models.Booth, BoothSummaryDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<BoothCreateDto, Models.Booth>()
            .ForMember(dest => dest.BoothID, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => BoothStatus.Available))
            .ForMember(dest => dest.IsMerged, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.MergeID, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentAreaSqM, opt => opt.MapFrom(src => src.OriginalAreaSqM))
            .ForMember(dest => dest.ShapeType, opt => opt.MapFrom(src => ParseShapeType(src.ShapeType)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Hall, opt => opt.Ignore())
            .ForMember(dest => dest.BoothMerge, opt => opt.Ignore());

        CreateMap<BoothUpdateDto, Models.Booth>()
            .ForMember(dest => dest.BoothID, opt => opt.Ignore())
            .ForMember(dest => dest.HallID, opt => opt.Ignore())
            .ForMember(dest => dest.OriginalAreaSqM, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentAreaSqM, opt => opt.Ignore())
            .ForMember(dest => dest.IsMerged, opt => opt.Ignore())
            .ForMember(dest => dest.MergeID, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<BoothStatus>(src.Status, true)))
            .ForMember(dest => dest.ShapeType, opt => opt.MapFrom(src => ParseNullableShapeType(src.ShapeType)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Hall, opt => opt.Ignore())
            .ForMember(dest => dest.BoothMerge, opt => opt.Ignore());

        CreateMap<BoothMerge, BoothMergeDto>()
            .ForMember(dest => dest.MergedAreaSqM, opt => opt.MapFrom(src => src.TotalAreaSqM))
            .ForMember(dest => dest.MergeDate, opt => opt.MapFrom(src => src.MergedAt))
            .ForMember(dest => dest.BoothItems, opt => opt.MapFrom(src => src.MergeItems))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.MergeItems != null && src.MergeItems.Any() && src.MergeItems.First().Booth != null ? src.MergeItems.First().Booth.HallID : 0))
            .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => src.MergeItems != null && src.MergeItems.Any() && src.MergeItems.First().Booth != null && src.MergeItems.First().Booth.Hall != null ? src.MergeItems.First().Booth.Hall.HallName : string.Empty));

        CreateMap<BoothMergeItem, BoothMergeItemDto>()
            .ForMember(dest => dest.MergeItemID, opt => opt.MapFrom(src => src.ItemID))
            .ForMember(dest => dest.AreaSqM, opt => opt.MapFrom(src => src.OriginalAreaSqM))
            .ForMember(dest => dest.BoothNumber, opt => opt.MapFrom(src => src.Booth != null ? src.Booth.BoothNumber : string.Empty));
    }

    private static BoothShapeType ParseShapeType(string? shapeType)
    {
        if (string.IsNullOrEmpty(shapeType)) return BoothShapeType.Rect;
        return Enum.TryParse<BoothShapeType>(shapeType, true, out var st) ? st : BoothShapeType.Rect;
    }

    private static BoothShapeType? ParseNullableShapeType(string? shapeType)
    {
        if (string.IsNullOrEmpty(shapeType)) return null;
        return Enum.TryParse<BoothShapeType>(shapeType, true, out var st) ? (BoothShapeType?)st : null;
    }
}
