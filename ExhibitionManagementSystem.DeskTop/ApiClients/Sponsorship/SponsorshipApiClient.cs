using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Sponsorship;

public class SponsorshipApiClient : ApiClientBase, ISponsorshipService
{
    public SponsorshipApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    #region Sponsors

    public Task<ServiceResult<IList<SponsorDto>>> GetSponsorsAsync(int tenantId)
    {
        return GetAsync<IList<SponsorDto>>("api/sponsorship/sponsors");
    }

    public Task<ServiceResult<SponsorDto>> GetSponsorByIdAsync(int tenantId, int sponsorId)
    {
        return GetAsync<SponsorDto>($"api/sponsorship/sponsors/{sponsorId}");
    }

    public Task<ServiceResult<SponsorDto>> CreateSponsorAsync(int tenantId, SponsorCreateDto dto)
    {
        return PostAsync<SponsorDto>("api/sponsorship/sponsors", dto);
    }

    public Task<ServiceResult<SponsorDto>> UpdateSponsorAsync(int tenantId, int sponsorId, SponsorUpdateDto dto)
    {
        return PutAsync<SponsorDto>($"api/sponsorship/sponsors/{sponsorId}", dto);
    }

    public Task<ServiceResult> DeleteSponsorAsync(int tenantId, int sponsorId, string userId)
    {
        return DeleteAsync($"api/sponsorship/sponsors/{sponsorId}");
    }

    #endregion

    #region Packages

    public Task<ServiceResult<IList<SponsorshipPackageDto>>> GetPackagesByExhibitionAsync(int tenantId, int exhibitionId)
    {
        return GetAsync<IList<SponsorshipPackageDto>>($"api/sponsorship/packages?exhibitionId={exhibitionId}");
    }

    public Task<ServiceResult<SponsorshipPackageDto>> GetPackageByIdAsync(int tenantId, int packageId)
    {
        return GetAsync<SponsorshipPackageDto>($"api/sponsorship/packages/{packageId}");
    }

    public Task<ServiceResult<SponsorshipPackageDto>> CreatePackageAsync(int tenantId, SponsorshipPackageCreateDto dto)
    {
        return PostAsync<SponsorshipPackageDto>("api/sponsorship/packages", dto);
    }

    public Task<ServiceResult<SponsorshipPackageDto>> UpdatePackageAsync(int tenantId, int packageId, SponsorshipPackageUpdateDto dto)
    {
        return PutAsync<SponsorshipPackageDto>($"api/sponsorship/packages/{packageId}", dto);
    }

    public Task<ServiceResult> DeletePackageAsync(int tenantId, int packageId, string userId)
    {
        return DeleteAsync($"api/sponsorship/packages/{packageId}");
    }

    #endregion

    #region Advertising Spaces

    public Task<ServiceResult<IList<AdvertisingSpaceDto>>> GetSpacesByExhibitionAsync(int tenantId, int exhibitionId, bool? availableOnly = null)
    {
        var url = availableOnly.HasValue
            ? $"api/sponsorship/spaces?exhibitionId={exhibitionId}&availableOnly={availableOnly.Value}"
            : $"api/sponsorship/spaces?exhibitionId={exhibitionId}";
        return GetAsync<IList<AdvertisingSpaceDto>>(url);
    }

    public Task<ServiceResult<AdvertisingSpaceDto>> GetSpaceByIdAsync(int tenantId, int spaceId)
    {
        return GetAsync<AdvertisingSpaceDto>($"api/sponsorship/spaces/{spaceId}");
    }

    public Task<ServiceResult<AdvertisingSpaceDto>> CreateSpaceAsync(int tenantId, AdvertisingSpaceCreateDto dto)
    {
        return PostAsync<AdvertisingSpaceDto>("api/sponsorship/spaces", dto);
    }

    public Task<ServiceResult<AdvertisingSpaceDto>> UpdateSpaceAsync(int tenantId, int spaceId, AdvertisingSpaceUpdateDto dto)
    {
        return PutAsync<AdvertisingSpaceDto>($"api/sponsorship/spaces/{spaceId}", dto);
    }

    public Task<ServiceResult> DeleteSpaceAsync(int tenantId, int spaceId, string userId)
    {
        return DeleteAsync($"api/sponsorship/spaces/{spaceId}");
    }

    #endregion

    #region Contracts

    public Task<ServiceResult<IList<SponsorshipContractDto>>> GetContractsByExhibitionAsync(int tenantId, int exhibitionId)
    {
        return GetAsync<IList<SponsorshipContractDto>>($"api/sponsorship/contracts?exhibitionId={exhibitionId}");
    }

    public Task<ServiceResult<SponsorshipContractDto>> GetContractByIdAsync(int tenantId, int contractId)
    {
        return GetAsync<SponsorshipContractDto>($"api/sponsorship/contracts/{contractId}");
    }

    public Task<ServiceResult<SponsorshipContractDto>> CreateContractAsync(int tenantId, string userId, SponsorshipContractCreateDto dto)
    {
        return PostAsync<SponsorshipContractDto>("api/sponsorship/contracts", dto);
    }

    public Task<ServiceResult<SponsorshipContractDto>> UpdateContractStatusAsync(int tenantId, int contractId, string status)
    {
        return PutAsync<SponsorshipContractDto>($"api/sponsorship/contracts/{contractId}/status", status);
    }

    public Task<ServiceResult> DeleteContractAsync(int tenantId, int contractId, string userId)
    {
        return DeleteAsync($"api/sponsorship/contracts/{contractId}");
    }

    public Task<ServiceResult<SponsorAdAssignmentDto>> AssignSpaceAsync(int tenantId, SponsorAdAssignmentCreateDto dto)
    {
        return PostAsync<SponsorAdAssignmentDto>("api/sponsorship/assign-space", dto);
    }

    public Task<ServiceResult> RemoveSpaceAssignmentAsync(int tenantId, int assignmentId, string userId)
    {
        return DeleteAsync($"api/sponsorship/assignments/{assignmentId}");
    }

    #endregion
}
