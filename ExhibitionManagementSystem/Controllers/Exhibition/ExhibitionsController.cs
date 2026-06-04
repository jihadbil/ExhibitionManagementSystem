using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Exhibition;

[Route("api/[controller]")]
public class ExhibitionsController : BaseApiController
{
    private readonly IExhibitionService _exhibitionService;

    public ExhibitionsController(IExhibitionService exhibitionService)
        => _exhibitionService = exhibitionService;

    // GET /api/exhibitions?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ExhibitionSummaryDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _exhibitionService.GetByTenantAsync(TenantId, page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/exhibitions/active
    [HttpGet("active")]
    public async Task<ActionResult<IList<ExhibitionSummaryDto>>> GetActive()
    {
        var result = await _exhibitionService.GetActiveAsync(TenantId);
        return ToActionResult(result);
    }

    // GET /api/exhibitions/upcoming?count=5
    [HttpGet("upcoming")]
    public async Task<ActionResult<IList<ExhibitionSummaryDto>>> GetUpcoming(
        [FromQuery] int count = 5)
    {
        var result = await _exhibitionService.GetUpcomingAsync(TenantId, count);
        return ToActionResult(result);
    }

    // GET /api/exhibitions/{exhibitionId}
    [HttpGet("{exhibitionId:int}")]
    public async Task<ActionResult<ExhibitionDto>> GetById(int exhibitionId)
    {
        var result = await _exhibitionService.GetByIdAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // POST /api/exhibitions
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ExhibitionDto>> Create(
        [FromBody] ExhibitionCreateDto dto)
    {
        var result = await _exhibitionService.CreateAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { exhibitionId = result.Data!.ExhibitionID }, result.Data);
    }

    // PUT /api/exhibitions/{exhibitionId}
    [HttpPut("{exhibitionId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ExhibitionDto>> Update(
        int exhibitionId,
        [FromBody] ExhibitionUpdateDto dto)
    {
        var result = await _exhibitionService.UpdateAsync(TenantId, exhibitionId, dto);
        return ToActionResult(result);
    }

    // PATCH /api/exhibitions/{exhibitionId}/status
    // Body: "Open" | "Closed" | "Draft"
    [HttpPatch("{exhibitionId:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ExhibitionDto>> ChangeStatus(
        int exhibitionId,
        [FromBody] string status)
    {
        var result = await _exhibitionService.ChangeStatusAsync(
            TenantId, exhibitionId, status);
        return ToActionResult(result);
    }

    // DELETE /api/exhibitions/{exhibitionId}
    [HttpDelete("{exhibitionId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int exhibitionId)
    {
        var result = await _exhibitionService.DeleteAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // ─── Schedules ────────────────────────────────────────────────────────────

    // GET /api/exhibitions/{exhibitionId}/schedules
    [HttpGet("{exhibitionId:int}/schedules")]
    public async Task<ActionResult<IList<ExhibitionScheduleDto>>> GetSchedules(
        int exhibitionId)
    {
        var result = await _exhibitionService.GetSchedulesAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // POST /api/exhibitions/{exhibitionId}/schedules
    [HttpPost("{exhibitionId:int}/schedules")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ExhibitionScheduleDto>> AddSchedule(
        int exhibitionId,
        [FromBody] ExhibitionScheduleCreateDto dto)
    {
        dto.ExhibitionID = exhibitionId;
        var result = await _exhibitionService.AddScheduleAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/exhibitions/schedules/{scheduleId}
    [HttpDelete("schedules/{scheduleId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RemoveSchedule(int scheduleId)
    {
        var result = await _exhibitionService.RemoveScheduleAsync(TenantId, scheduleId);
        return ToActionResult(result);
    }
}
