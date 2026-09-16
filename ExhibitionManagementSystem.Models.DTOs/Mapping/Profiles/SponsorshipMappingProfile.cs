using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class SponsorshipMappingProfile : Profile
{
    public SponsorshipMappingProfile()
    {
        CreateMap<Sponsor, SponsorDto>()
            .ForMember(dest => dest.TotalContractsCount, opt => opt.MapFrom(src => src.Contracts.Count));
        CreateMap<SponsorCreateDto, Sponsor>();
        CreateMap<SponsorUpdateDto, Sponsor>();

        CreateMap<SponsorshipPackage, SponsorshipPackageDto>()
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition.Name))
            .ForMember(dest => dest.AllocatedSponsorsCount, opt => opt.MapFrom(src => src.Contracts.Count));
        CreateMap<SponsorshipPackageCreateDto, SponsorshipPackage>();
        CreateMap<SponsorshipPackageUpdateDto, SponsorshipPackage>();

        CreateMap<AdvertisingSpace, AdvertisingSpaceDto>()
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition.Name))
            .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue != null ? src.Venue.Name : null))
            .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => src.Hall != null ? src.Hall.HallName : null))
            .ForMember(dest => dest.CurrentSponsorName, opt => opt.MapFrom(src =>
                src.AdAssignments.OrderByDescending(a => a.AssignedDate).Select(a => a.Contract.Sponsor.CompanyName).FirstOrDefault()));
        CreateMap<AdvertisingSpaceCreateDto, AdvertisingSpace>();
        CreateMap<AdvertisingSpaceUpdateDto, AdvertisingSpace>();

        CreateMap<SponsorshipContract, SponsorshipContractDto>()
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition.Name))
            .ForMember(dest => dest.SponsorCompanyName, opt => opt.MapFrom(src => src.Sponsor.CompanyName))
            .ForMember(dest => dest.SponsorLogoUrl, opt => opt.MapFrom(src => src.Sponsor.LogoUrl))
            .ForMember(dest => dest.PackageName, opt => opt.MapFrom(src => src.Package != null ? src.Package.PackageName : null))
            .ForMember(dest => dest.AdAssignments, opt => opt.MapFrom(src => src.AdAssignments));
        CreateMap<SponsorshipContractCreateDto, SponsorshipContract>();

        CreateMap<SponsorAdAssignment, SponsorAdAssignmentDto>()
            .ForMember(dest => dest.SpaceCode, opt => opt.MapFrom(src => src.Space.SpaceCode))
            .ForMember(dest => dest.SpaceName, opt => opt.MapFrom(src => src.Space.SpaceName))
            .ForMember(dest => dest.SpaceType, opt => opt.MapFrom(src => src.Space.SpaceType));
        CreateMap<SponsorAdAssignmentCreateDto, SponsorAdAssignment>();
    }
}
