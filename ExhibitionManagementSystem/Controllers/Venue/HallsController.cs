using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Venue;

[Route("api/[controller]")]
public class HallsController : BaseApiController
{
    private readonly IHallService _hallService;

    public HallsController(IHallService hallService)
        => _hallService = hallService;

    // GET /api/halls/venue/{venueId}
    [HttpGet("venue/{venueId:int}")]
    public async Task<ActionResult<IList<HallDto>>> GetByVenue(int venueId)
    {
        var result = await _hallService.GetByVenueAsync(TenantId, venueId);
        return ToActionResult(result);
    }

    // GET /api/halls/{hallId}
    [HttpGet("{hallId:int}")]
    public async Task<ActionResult<HallDto>> GetById(int hallId)
    {
        var result = await _hallService.GetByIdAsync(TenantId, hallId);
        return ToActionResult(result);
    }

    // POST /api/halls
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<HallDto>> Create(
        [FromBody] HallCreateDto dto)
    {
        var result = await _hallService.CreateAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { hallId = result.Data!.HallID }, result.Data);
    }

    // PUT /api/halls/{hallId}
    [HttpPut("{hallId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<HallDto>> Update(
        int hallId,
        [FromBody] HallUpdateDto dto)
    {
        var result = await _hallService.UpdateAsync(TenantId, hallId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/halls/{hallId}
    [HttpDelete("{hallId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int hallId)
    {
        var result = await _hallService.DeleteAsync(TenantId, hallId);
        return ToActionResult(result);
    }
}
