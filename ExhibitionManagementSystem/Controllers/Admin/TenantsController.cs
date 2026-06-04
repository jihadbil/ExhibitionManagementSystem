using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Admin;

[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class TenantsController : BaseApiController
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
        => _tenantService = tenantService;

    // GET /api/tenants?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TenantDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _tenantService.GetAllAsync(page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/tenants/{tenantId}
    [HttpGet("{tenantId:int}")]
    public async Task<ActionResult<TenantDto>> GetById(int tenantId)
    {
        var result = await _tenantService.GetByIdAsync(tenantId);
        return ToActionResult(result);
    }

    // POST /api/tenants
    [HttpPost]
    public async Task<ActionResult<TenantDto>> Create(
        [FromBody] TenantCreateDto dto)
    {
        var result = await _tenantService.CreateAsync(dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { tenantId = result.Data!.TenantID }, result.Data);
    }

    // PUT /api/tenants/{tenantId}
    [HttpPut("{tenantId:int}")]
    public async Task<ActionResult<TenantDto>> Update(
        int tenantId,
        [FromBody] TenantUpdateDto dto)
    {
        var result = await _tenantService.UpdateAsync(tenantId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/tenants/{tenantId}
    [HttpDelete("{tenantId:int}")]
    public async Task<IActionResult> Delete(int tenantId)
    {
        var result = await _tenantService.DeleteAsync(tenantId);
        return ToActionResult(result);
    }

    // GET /api/tenants/{tenantId}/subscription
    [HttpGet("{tenantId:int}/subscription")]
    public async Task<ActionResult<TenantSubscriptionDto>> GetActiveSubscription(
        int tenantId)
    {
        var result = await _tenantService.GetActiveSubscriptionAsync(tenantId);
        return ToActionResult(result);
    }
}
