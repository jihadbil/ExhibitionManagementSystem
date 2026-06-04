using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Booth;

[Route("api/[controller]")]
public class BoothsController : BaseApiController
{
    private readonly IBoothService _boothService;

    public BoothsController(IBoothService boothService)
        => _boothService = boothService;

    // GET /api/booths/hall/{hallId}
    [HttpGet("hall/{hallId:int}")]
    public async Task<ActionResult<IList<BoothDto>>> GetByHall(int hallId)
    {
        var result = await _boothService.GetByHallAsync(TenantId, hallId);
        return ToActionResult(result);
    }

    // GET /api/booths/available?hallId=5&exhibitionId=3
    [HttpGet("available")]
    public async Task<ActionResult<IList<BoothSummaryDto>>> GetAvailable(
        [FromQuery] int hallId,
        [FromQuery] int exhibitionId)
    {
        var result = await _boothService.GetAvailableAsync(
            TenantId, hallId, exhibitionId);
        return ToActionResult(result);
    }

    // GET /api/booths/{boothId}
    [HttpGet("{boothId:int}")]
    public async Task<ActionResult<BoothDto>> GetById(int boothId)
    {
        var result = await _boothService.GetByIdAsync(TenantId, boothId);
        return ToActionResult(result);
    }

    // POST /api/booths
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothDto>> Create(
        [FromBody] BoothCreateDto dto)
    {
        var result = await _boothService.CreateAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { boothId = result.Data!.BoothID }, result.Data);
    }

    // PUT /api/booths/{boothId}
    [HttpPut("{boothId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothDto>> Update(
        int boothId,
        [FromBody] BoothUpdateDto dto)
    {
        var result = await _boothService.UpdateAsync(TenantId, boothId, dto);
        return ToActionResult(result);
    }

    // POST /api/booths/merge
    [HttpPost("merge")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothMergeDto>> Merge(
        [FromBody] BoothMergeCreateDto dto)
    {
        var result = await _boothService.MergeBoothsAsync(TenantId, UserId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/booths/merge/{mergeId}
    [HttpDelete("merge/{mergeId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Unmerge(int mergeId)
    {
        var result = await _boothService.UnmergeBoothsAsync(TenantId, mergeId);
        return ToActionResult(result);
    }
}
