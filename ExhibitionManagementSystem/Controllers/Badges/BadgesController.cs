using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Controllers.Badges;

[Route("api/[controller]")]
public class BadgesController : BaseApiController
{
    private readonly IBadgeService _badgeService;

    public BadgesController(IBadgeService badgeService)
    {
        _badgeService = badgeService;
    }

    // GET /api/badges/templates?exhibitionId=1
    [HttpGet("templates")]
    public async Task<ActionResult<IList<BadgeTemplateDto>>> GetTemplates([FromQuery] int? exhibitionId)
    {
        var result = await _badgeService.GetTemplatesAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // GET /api/badges/templates/{templateId}
    [HttpGet("templates/{templateId:int}")]
    public async Task<ActionResult<BadgeTemplateDto>> GetTemplateById(int templateId)
    {
        var result = await _badgeService.GetTemplateByIdAsync(TenantId, templateId);
        return ToActionResult(result);
    }

    // POST /api/badges/templates
    [HttpPost("templates")]
    public async Task<ActionResult<BadgeTemplateDto>> CreateTemplate([FromBody] BadgeTemplateCreateDto dto)
    {
        var result = await _badgeService.CreateTemplateAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // PUT /api/badges/templates/{templateId}
    [HttpPut("templates/{templateId:int}")]
    public async Task<ActionResult<BadgeTemplateDto>> UpdateTemplate(int templateId, [FromBody] BadgeTemplateUpdateDto dto)
    {
        var result = await _badgeService.UpdateTemplateAsync(TenantId, templateId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/badges/templates/{templateId}
    [HttpDelete("templates/{templateId:int}")]
    public async Task<ActionResult> DeleteTemplate(int templateId)
    {
        var result = await _badgeService.DeleteTemplateAsync(TenantId, templateId, UserId);
        return ToActionResult(result);
    }

    // GET /api/badges/render-by-qr/{qrCode}
    [HttpGet("render-by-qr/{qrCode}")]
    public async Task<ActionResult<BadgePrintPayloadDto>> RenderByQRCode(string qrCode)
    {
        var result = await _badgeService.GenerateBadgePayloadByQRCodeAsync(TenantId, qrCode);
        return ToActionResult(result);
    }

    // GET /api/badges/render-visitor/{visitorId}/{exhibitionId}
    [HttpGet("render-visitor/{visitorId:int}/{exhibitionId:int}")]
    public async Task<ActionResult<BadgePrintPayloadDto>> RenderVisitor(int visitorId, int exhibitionId)
    {
        var result = await _badgeService.GenerateBadgePayloadForVisitorAsync(TenantId, visitorId, exhibitionId);
        return ToActionResult(result);
    }

    // GET /api/badges/render-staff/{staffId}
    [HttpGet("render-staff/{staffId:int}")]
    public async Task<ActionResult<BadgePrintPayloadDto>> RenderStaff(int staffId)
    {
        var result = await _badgeService.GenerateBadgePayloadForStaffAsync(TenantId, staffId);
        return ToActionResult(result);
    }
}
