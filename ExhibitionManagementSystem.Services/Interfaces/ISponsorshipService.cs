using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces;

public interface ISponsorshipService
{
    // Sponsors
    Task<ServiceResult<IList<SponsorDto>>> GetSponsorsAsync(int tenantId);
    Task<ServiceResult<SponsorDto>> GetSponsorByIdAsync(int tenantId, int sponsorId);
    Task<ServiceResult<SponsorDto>> CreateSponsorAsync(int tenantId, SponsorCreateDto dto);
    Task<ServiceResult<SponsorDto>> UpdateSponsorAsync(int tenantId, int sponsorId, SponsorUpdateDto dto);
    Task<ServiceResult> DeleteSponsorAsync(int tenantId, int sponsorId, string userId);

    // Packages
    Task<ServiceResult<IList<SponsorshipPackageDto>>> GetPackagesByExhibitionAsync(int tenantId, int exhibitionId);
    Task<ServiceResult<SponsorshipPackageDto>> GetPackageByIdAsync(int tenantId, int packageId);
    Task<ServiceResult<SponsorshipPackageDto>> CreatePackageAsync(int tenantId, SponsorshipPackageCreateDto dto);
    Task<ServiceResult<SponsorshipPackageDto>> UpdatePackageAsync(int tenantId, int packageId, SponsorshipPackageUpdateDto dto);
    Task<ServiceResult> DeletePackageAsync(int tenantId, int packageId, string userId);

    // Advertising Spaces
    Task<ServiceResult<IList<AdvertisingSpaceDto>>> GetSpacesByExhibitionAsync(int tenantId, int exhibitionId, bool? availableOnly = null);
    Task<ServiceResult<AdvertisingSpaceDto>> GetSpaceByIdAsync(int tenantId, int spaceId);
    Task<ServiceResult<AdvertisingSpaceDto>> CreateSpaceAsync(int tenantId, AdvertisingSpaceCreateDto dto);
    Task<ServiceResult<AdvertisingSpaceDto>> UpdateSpaceAsync(int tenantId, int spaceId, AdvertisingSpaceUpdateDto dto);
    Task<ServiceResult> DeleteSpaceAsync(int tenantId, int spaceId, string userId);

    // Contracts
    Task<ServiceResult<IList<SponsorshipContractDto>>> GetContractsByExhibitionAsync(int tenantId, int exhibitionId);
    Task<ServiceResult<SponsorshipContractDto>> GetContractByIdAsync(int tenantId, int contractId);
    Task<ServiceResult<SponsorshipContractDto>> CreateContractAsync(int tenantId, string userId, SponsorshipContractCreateDto dto);
    Task<ServiceResult<SponsorshipContractDto>> UpdateContractStatusAsync(int tenantId, int contractId, string status);
    Task<ServiceResult> DeleteContractAsync(int tenantId, int contractId, string userId);

    // Space Assignments
    Task<ServiceResult<SponsorAdAssignmentDto>> AssignSpaceAsync(int tenantId, SponsorAdAssignmentCreateDto dto);
    Task<ServiceResult> RemoveSpaceAssignmentAsync(int tenantId, int assignmentId, string userId);
}
