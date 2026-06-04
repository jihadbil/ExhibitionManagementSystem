using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Exhibition;

[Route("api/[controller]")]
public class ExhibitorsController : BaseApiController
{
    private readonly IExhibitorService _exhibitorService;

    public ExhibitorsController(IExhibitorService exhibitorService)
        => _exhibitorService = exhibitorService;

    // GET /api/exhibitors?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ExhibitorSummaryDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _exhibitorService.GetByTenantAsync(TenantId, page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/exhibitors/search?term=كلمة
    [HttpGet("search")]
    public async Task<ActionResult<IList<ExhibitorSummaryDto>>> Search(
        [FromQuery] string term)
    {
        var result = await _exhibitorService.SearchAsync(TenantId, term);
        return ToActionResult(result);
    }

    // GET /api/exhibitors/{exhibitorId}
    [HttpGet("{exhibitorId:int}")]
    public async Task<ActionResult<ExhibitorDto>> GetById(int exhibitorId)
    {
        var result = await _exhibitorService.GetByIdAsync(TenantId, exhibitorId);
        return ToActionResult(result);
    }

    // GET /api/exhibitors/by-user/{userId}
    [HttpGet("by-user/{userId}")]
    public async Task<ActionResult<ExhibitorDto>> GetByUserId(string userId)
    {
        var result = await _exhibitorService.GetByUserIdAsync(TenantId, userId);
        return ToActionResult(result);
    }

    // GET /api/exhibitors/{exhibitorId}/reservations
    [HttpGet("{exhibitorId:int}/reservations")]
    public async Task<ActionResult<IList<BoothReservationSummaryDto>>> GetReservations(
        int exhibitorId)
    {
        var result = await _exhibitorService.GetReservationsAsync(TenantId, exhibitorId);
        return ToActionResult(result);
    }

    // POST /api/exhibitors
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ExhibitorDto>> Create(
        [FromBody] ExhibitorCreateDto dto)
    {
        var result = await _exhibitorService.CreateAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { exhibitorId = result.Data!.ExhibitorID }, result.Data);
    }

    // PUT /api/exhibitors/{exhibitorId}
    [HttpPut("{exhibitorId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ExhibitorDto>> Update(
        int exhibitorId,
        [FromBody] ExhibitorUpdateDto dto)
    {
        var result = await _exhibitorService.UpdateAsync(TenantId, exhibitorId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/exhibitors/{exhibitorId}
    [HttpDelete("{exhibitorId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int exhibitorId)
    {
        var result = await _exhibitorService.DeleteAsync(TenantId, exhibitorId);
        return ToActionResult(result);
    }
}
