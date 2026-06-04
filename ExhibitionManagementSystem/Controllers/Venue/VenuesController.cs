using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Venue;

[Route("api/[controller]")]
public class VenuesController : BaseApiController
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
        => _venueService = venueService;

    // GET /api/venues
    [HttpGet]
    public async Task<ActionResult<IList<VenueDto>>> GetAll()
    {
        var result = await _venueService.GetByTenantAsync(TenantId);
        return ToActionResult(result);
    }

    // GET /api/venues/summaries
    [HttpGet("summaries")]
    public async Task<ActionResult<IList<VenueSummaryDto>>> GetSummaries()
    {
        var result = await _venueService.GetSummariesAsync(TenantId);
        return ToActionResult(result);
    }

    // GET /api/venues/{venueId}
    [HttpGet("{venueId:int}")]
    public async Task<ActionResult<VenueDto>> GetById(int venueId)
    {
        var result = await _venueService.GetByIdAsync(TenantId, venueId);
        return ToActionResult(result);
    }

    // POST /api/venues
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<VenueDto>> Create(
        [FromBody] VenueCreateDto dto)
    {
        var result = await _venueService.CreateAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { venueId = result.Data!.VenueID }, result.Data);
    }

    // PUT /api/venues/{venueId}
    [HttpPut("{venueId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<VenueDto>> Update(
        int venueId,
        [FromBody] VenueUpdateDto dto)
    {
        var result = await _venueService.UpdateAsync(TenantId, venueId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/venues/{venueId}
    [HttpDelete("{venueId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int venueId)
    {
        var result = await _venueService.DeleteAsync(TenantId, venueId);
        return ToActionResult(result);
    }
}
