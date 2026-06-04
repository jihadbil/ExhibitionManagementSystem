using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Visitor;

[Route("api/[controller]")]
public class VisitorsController : BaseApiController
{
    private readonly IVisitorService _visitorService;

    public VisitorsController(IVisitorService visitorService)
        => _visitorService = visitorService;

    // GET /api/visitors?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<VisitorDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _visitorService.GetByTenantAsync(TenantId, page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/visitors/search?term=كلمة
    [HttpGet("search")]
    public async Task<ActionResult<IList<VisitorDto>>> Search(
        [FromQuery] string term)
    {
        var result = await _visitorService.SearchAsync(TenantId, term);
        return ToActionResult(result);
    }

    // GET /api/visitors/{visitorId}
    [HttpGet("{visitorId:int}")]
    public async Task<ActionResult<VisitorDto>> GetById(int visitorId)
    {
        var result = await _visitorService.GetByIdAsync(TenantId, visitorId);
        return ToActionResult(result);
    }

    // POST /api/visitors/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<VisitorDto>> Register(
        [FromBody] VisitorCreateDto dto)
    {
        var result = await _visitorService.RegisterAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { visitorId = result.Data!.VisitorID }, result.Data);
    }

    // POST /api/visitors/{visitorId}/ratings
    // Body: { "exhibitionId": 1, "rating": 4, "comment": "رائع!" }
    [HttpPost("{visitorId:int}/ratings")]
    public async Task<ActionResult<VisitorRatingDto>> SubmitRating(
        int visitorId,
        [FromBody] SubmitRatingRequest request)
    {
        var result = await _visitorService.SubmitRatingAsync(
            TenantId, visitorId,
            request.ExhibitionId, request.Rating, request.Comment);
        return ToActionResult(result);
    }

    // GET /api/visitors/ratings/exhibition/{exhibitionId}
    [HttpGet("ratings/exhibition/{exhibitionId:int}")]
    public async Task<ActionResult<VisitorRatingSummaryDto>> GetRatingSummary(
        int exhibitionId)
    {
        var result = await _visitorService.GetRatingSummaryAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }
}

public record SubmitRatingRequest(int ExhibitionId, int Rating, string? Comment);
