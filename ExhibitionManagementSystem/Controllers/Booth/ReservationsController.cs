using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Booth;

[Route("api/[controller]")]
public class ReservationsController : BaseApiController
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
        => _reservationService = reservationService;

    // GET /api/reservations/exhibition/{exhibitionId}?page=1&pageSize=20
    [HttpGet("exhibition/{exhibitionId:int}")]
    public async Task<ActionResult<PagedResultDto<BoothReservationSummaryDto>>> GetByExhibition(
        int exhibitionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reservationService.GetByExhibitionAsync(
            TenantId, exhibitionId, page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/reservations/exhibitor/{exhibitorId}
    [HttpGet("exhibitor/{exhibitorId:int}")]
    public async Task<ActionResult<IList<BoothReservationSummaryDto>>> GetByExhibitor(
        int exhibitorId)
    {
        var result = await _reservationService.GetByExhibitorAsync(
            TenantId, exhibitorId);
        return ToActionResult(result);
    }

    // GET /api/reservations/exhibition/{exhibitionId}/unpaid
    [HttpGet("exhibition/{exhibitionId:int}/unpaid")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IList<BoothReservationSummaryDto>>> GetUnpaid(
        int exhibitionId)
    {
        var result = await _reservationService.GetUnpaidAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // GET /api/reservations/{reservationId}
    [HttpGet("{reservationId:int}")]
    public async Task<ActionResult<BoothReservationDto>> GetById(int reservationId)
    {
        var result = await _reservationService.GetByIdAsync(TenantId, reservationId);
        return ToActionResult(result);
    }

    // POST /api/reservations
    // يحسب السعر تلقائياً عبر IPricingService ويُنفّذ Transaction كاملة
    [HttpPost]
    public async Task<ActionResult<BoothReservationDto>> Create(
        [FromBody] BoothReservationCreateDto dto)
    {
        var result = await _reservationService.CreateAsync(TenantId, UserId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { reservationId = result.Data!.ReservationID }, result.Data);
    }

    // PUT /api/reservations/{reservationId}
    [HttpPut("{reservationId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothReservationDto>> Update(
        int reservationId,
        [FromBody] BoothReservationUpdateDto dto)
    {
        var result = await _reservationService.UpdateAsync(TenantId, reservationId, dto);
        return ToActionResult(result);
    }

    // PATCH /api/reservations/{reservationId}/approve
    [HttpPatch("{reservationId:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothReservationDto>> Approve(int reservationId)
    {
        var result = await _reservationService.ApproveAsync(TenantId, reservationId);
        return ToActionResult(result);
    }

    // PATCH /api/reservations/{reservationId}/cancel
    [HttpPatch("{reservationId:int}/cancel")]
    public async Task<IActionResult> Cancel(int reservationId)
    {
        var result = await _reservationService.CancelAsync(TenantId, reservationId);
        return ToActionResult(result);
    }

    // ─── Reservation Services ─────────────────────────────────────────────────

    // POST /api/reservations/{reservationId}/services
    [HttpPost("{reservationId:int}/services")]
    public async Task<ActionResult<ReservationServiceDto>> AddService(
        int reservationId,
        [FromBody] ReservationServiceCreateDto dto)
    {
        var result = await _reservationService.AddServiceToReservationAsync(
            TenantId, reservationId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/reservations/{reservationId}/services/{rsId}
    [HttpDelete("{reservationId:int}/services/{rsId:int}")]
    public async Task<IActionResult> RemoveService(int reservationId, int rsId)
    {
        var result = await _reservationService.RemoveServiceFromReservationAsync(
            TenantId, reservationId, rsId);
        return ToActionResult(result);
    }
}
