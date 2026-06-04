using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Services;

[Route("api/[controller]")]
public class PricingController : BaseApiController
{
    private readonly IPricingService _pricingService;

    public PricingController(IPricingService pricingService)
        => _pricingService = pricingService;

    // ─── Booth Price Rules ────────────────────────────────────────────────────

    // GET /api/pricing/booth-rules?exhibitionId=3
    [HttpGet("booth-rules")]
    public async Task<ActionResult<IList<BoothPriceRuleDto>>> GetBoothRules(
        [FromQuery] int? exhibitionId = null)
    {
        var result = await _pricingService.GetBoothPriceRulesAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // POST /api/pricing/booth-rules
    [HttpPost("booth-rules")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothPriceRuleDto>> CreateBoothRule(
        [FromBody] BoothPriceRuleCreateDto dto)
    {
        var result = await _pricingService.CreateBoothPriceRuleAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // PUT /api/pricing/booth-rules/{ruleId}
    [HttpPut("booth-rules/{ruleId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothPriceRuleDto>> UpdateBoothRule(
        int ruleId,
        [FromBody] BoothPriceRuleCreateDto dto)
    {
        var result = await _pricingService.UpdateBoothPriceRuleAsync(TenantId, ruleId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/pricing/booth-rules/{ruleId}
    [HttpDelete("booth-rules/{ruleId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteBoothRule(int ruleId)
    {
        var result = await _pricingService.DeleteBoothPriceRuleAsync(TenantId, ruleId);
        return ToActionResult(result);
    }

    // ─── Service Price Rules ──────────────────────────────────────────────────

    // GET /api/pricing/service-rules?exhibitionId=3
    [HttpGet("service-rules")]
    public async Task<ActionResult<IList<ServicePriceRuleDto>>> GetServiceRules(
        [FromQuery] int? exhibitionId = null)
    {
        var result = await _pricingService.GetServicePriceRulesAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // POST /api/pricing/service-rules
    [HttpPost("service-rules")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServicePriceRuleDto>> CreateServiceRule(
        [FromBody] ServicePriceRuleCreateDto dto)
    {
        var result = await _pricingService.CreateServicePriceRuleAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // ─── Packages ─────────────────────────────────────────────────────────────

    // GET /api/pricing/packages
    [HttpGet("packages")]
    public async Task<ActionResult<IList<PricingPackageDto>>> GetPackages()
    {
        var result = await _pricingService.GetPackagesAsync(TenantId);
        return ToActionResult(result);
    }

    // POST /api/pricing/packages
    [HttpPost("packages")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PricingPackageDto>> CreatePackage(
        [FromBody] PricingPackageCreateDto dto)
    {
        var result = await _pricingService.CreatePackageAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // ─── Price Calculators (Preview) ──────────────────────────────────────────

    // POST /api/pricing/calculate/booth
    // Body: { "exhibitionId": 1, "boothType": "Standard", "exhibitorCategory": "Technology", "areaSqM": 25 }
    [HttpPost("calculate/booth")]
    public async Task<ActionResult<decimal>> CalculateBoothPrice(
        [FromBody] CalculateBoothPriceRequest request)
    {
        if (!Enum.TryParse<ExhibitionManagementSystem.Models.Enums.BoothType>(
                request.BoothType, out var boothType))
            return BadRequest(new { ErrorMessage = "نوع الكشك غير صالح", ErrorCode = "INVALID_BOOTH_TYPE" });

        if (!Enum.TryParse<ExhibitionManagementSystem.Models.Enums.ExhibitorCategory>(
                request.ExhibitorCategory, out var category))
            return BadRequest(new { ErrorMessage = "فئة العارض غير صالحة", ErrorCode = "INVALID_EXHIBITOR_CATEGORY" });

        var result = await _pricingService.CalculateBoothPriceAsync(
            TenantId, request.ExhibitionId, boothType, category, request.AreaSqM ?? 0);
        return ToActionResult(result);
    }

    // POST /api/pricing/calculate/service
    // Body: { "serviceId": 1, "exhibitionId": 1, "quantity": 2 }
    [HttpPost("calculate/service")]
    public async Task<ActionResult<decimal>> CalculateServicePrice(
        [FromBody] CalculateServicePriceRequest request)
    {
        var result = await _pricingService.CalculateServicePriceAsync(
            TenantId, request.ServiceId, request.ExhibitionId, request.Quantity);
        return ToActionResult(result);
    }
}

// Request DTOs مضمّنة (بديلة لملف منفصل)
public record CalculateBoothPriceRequest(
    int ExhibitionId,
    string BoothType,
    string ExhibitorCategory,
    decimal? AreaSqM);

public record CalculateServicePriceRequest(
    int ServiceId,
    int ExhibitionId,
    int Quantity);
