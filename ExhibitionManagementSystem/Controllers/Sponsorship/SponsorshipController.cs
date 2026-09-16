using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Controllers.Sponsorship;

[Route("api/[controller]")]
public class SponsorshipController : BaseApiController
{
    private readonly ISponsorshipService _sponsorshipService;

    public SponsorshipController(ISponsorshipService sponsorshipService)
    {
        _sponsorshipService = sponsorshipService;
    }

    #region Sponsors

    // GET /api/sponsorship/sponsors
    [HttpGet("sponsors")]
    public async Task<ActionResult<IList<SponsorDto>>> GetSponsors()
    {
        var result = await _sponsorshipService.GetSponsorsAsync(TenantId);
        return ToActionResult(result);
    }

    // GET /api/sponsorship/sponsors/{sponsorId}
    [HttpGet("sponsors/{sponsorId:int}")]
    public async Task<ActionResult<SponsorDto>> GetSponsorById(int sponsorId)
    {
        var result = await _sponsorshipService.GetSponsorByIdAsync(TenantId, sponsorId);
        return ToActionResult(result);
    }

    // POST /api/sponsorship/sponsors
    [HttpPost("sponsors")]
    public async Task<ActionResult<SponsorDto>> CreateSponsor([FromBody] SponsorCreateDto dto)
    {
        var result = await _sponsorshipService.CreateSponsorAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // PUT /api/sponsorship/sponsors/{sponsorId}
    [HttpPut("sponsors/{sponsorId:int}")]
    public async Task<ActionResult<SponsorDto>> UpdateSponsor(int sponsorId, [FromBody] SponsorUpdateDto dto)
    {
        var result = await _sponsorshipService.UpdateSponsorAsync(TenantId, sponsorId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/sponsorship/sponsors/{sponsorId}
    [HttpDelete("sponsors/{sponsorId:int}")]
    public async Task<ActionResult> DeleteSponsor(int sponsorId)
    {
        var result = await _sponsorshipService.DeleteSponsorAsync(TenantId, sponsorId, UserId);
        return ToActionResult(result);
    }

    #endregion

    #region Packages

    // GET /api/sponsorship/packages?exhibitionId=1
    [HttpGet("packages")]
    public async Task<ActionResult<IList<SponsorshipPackageDto>>> GetPackages([FromQuery] int exhibitionId)
    {
        var result = await _sponsorshipService.GetPackagesByExhibitionAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // GET /api/sponsorship/packages/{packageId}
    [HttpGet("packages/{packageId:int}")]
    public async Task<ActionResult<SponsorshipPackageDto>> GetPackageById(int packageId)
    {
        var result = await _sponsorshipService.GetPackageByIdAsync(TenantId, packageId);
        return ToActionResult(result);
    }

    // POST /api/sponsorship/packages
    [HttpPost("packages")]
    public async Task<ActionResult<SponsorshipPackageDto>> CreatePackage([FromBody] SponsorshipPackageCreateDto dto)
    {
        var result = await _sponsorshipService.CreatePackageAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // PUT /api/sponsorship/packages/{packageId}
    [HttpPut("packages/{packageId:int}")]
    public async Task<ActionResult<SponsorshipPackageDto>> UpdatePackage(int packageId, [FromBody] SponsorshipPackageUpdateDto dto)
    {
        var result = await _sponsorshipService.UpdatePackageAsync(TenantId, packageId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/sponsorship/packages/{packageId}
    [HttpDelete("packages/{packageId:int}")]
    public async Task<ActionResult> DeletePackage(int packageId)
    {
        var result = await _sponsorshipService.DeletePackageAsync(TenantId, packageId, UserId);
        return ToActionResult(result);
    }

    #endregion

    #region Advertising Spaces

    // GET /api/sponsorship/spaces?exhibitionId=1&availableOnly=true
    [HttpGet("spaces")]
    public async Task<ActionResult<IList<AdvertisingSpaceDto>>> GetSpaces([FromQuery] int exhibitionId, [FromQuery] bool? availableOnly)
    {
        var result = await _sponsorshipService.GetSpacesByExhibitionAsync(TenantId, exhibitionId, availableOnly);
        return ToActionResult(result);
    }

    // GET /api/sponsorship/spaces/{spaceId}
    [HttpGet("spaces/{spaceId:int}")]
    public async Task<ActionResult<AdvertisingSpaceDto>> GetSpaceById(int spaceId)
    {
        var result = await _sponsorshipService.GetSpaceByIdAsync(TenantId, spaceId);
        return ToActionResult(result);
    }

    // POST /api/sponsorship/spaces
    [HttpPost("spaces")]
    public async Task<ActionResult<AdvertisingSpaceDto>> CreateSpace([FromBody] AdvertisingSpaceCreateDto dto)
    {
        var result = await _sponsorshipService.CreateSpaceAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // PUT /api/sponsorship/spaces/{spaceId}
    [HttpPut("spaces/{spaceId:int}")]
    public async Task<ActionResult<AdvertisingSpaceDto>> UpdateSpace(int spaceId, [FromBody] AdvertisingSpaceUpdateDto dto)
    {
        var result = await _sponsorshipService.UpdateSpaceAsync(TenantId, spaceId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/sponsorship/spaces/{spaceId}
    [HttpDelete("spaces/{spaceId:int}")]
    public async Task<ActionResult> DeleteSpace(int spaceId)
    {
        var result = await _sponsorshipService.DeleteSpaceAsync(TenantId, spaceId, UserId);
        return ToActionResult(result);
    }

    #endregion

    #region Contracts

    // GET /api/sponsorship/contracts?exhibitionId=1
    [HttpGet("contracts")]
    public async Task<ActionResult<IList<SponsorshipContractDto>>> GetContracts([FromQuery] int exhibitionId)
    {
        var result = await _sponsorshipService.GetContractsByExhibitionAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // GET /api/sponsorship/contracts/{contractId}
    [HttpGet("contracts/{contractId:int}")]
    public async Task<ActionResult<SponsorshipContractDto>> GetContractById(int contractId)
    {
        var result = await _sponsorshipService.GetContractByIdAsync(TenantId, contractId);
        return ToActionResult(result);
    }

    // POST /api/sponsorship/contracts
    [HttpPost("contracts")]
    public async Task<ActionResult<SponsorshipContractDto>> CreateContract([FromBody] SponsorshipContractCreateDto dto)
    {
        var result = await _sponsorshipService.CreateContractAsync(TenantId, UserId, dto);
        return ToActionResult(result);
    }

    // PUT /api/sponsorship/contracts/{contractId}/status
    [HttpPut("contracts/{contractId:int}/status")]
    public async Task<ActionResult<SponsorshipContractDto>> UpdateContractStatus(int contractId, [FromBody] string status)
    {
        var result = await _sponsorshipService.UpdateContractStatusAsync(TenantId, contractId, status);
        return ToActionResult(result);
    }

    // DELETE /api/sponsorship/contracts/{contractId}
    [HttpDelete("contracts/{contractId:int}")]
    public async Task<ActionResult> DeleteContract(int contractId)
    {
        var result = await _sponsorshipService.DeleteContractAsync(TenantId, contractId, UserId);
        return ToActionResult(result);
    }

    // POST /api/sponsorship/assign-space
    [HttpPost("assign-space")]
    public async Task<ActionResult<SponsorAdAssignmentDto>> AssignSpace([FromBody] SponsorAdAssignmentCreateDto dto)
    {
        var result = await _sponsorshipService.AssignSpaceAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/sponsorship/assignments/{assignmentId}
    [HttpDelete("assignments/{assignmentId:int}")]
    public async Task<ActionResult> RemoveAssignment(int assignmentId)
    {
        var result = await _sponsorshipService.RemoveSpaceAssignmentAsync(TenantId, assignmentId, UserId);
        return ToActionResult(result);
    }

    #endregion
}
