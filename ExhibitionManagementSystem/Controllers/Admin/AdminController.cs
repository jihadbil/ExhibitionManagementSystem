using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Admin;

[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : BaseApiController
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
        => _adminService = adminService;

    // GET /api/admin/audit-logs?page=1&pageSize=50
    [HttpGet("audit-logs")]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _adminService.GetAuditLogsAsync(TenantId, page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/admin/audit-logs/{tableName}/{recordId}
    [HttpGet("audit-logs/{tableName}/{recordId}")]
    public async Task<ActionResult<IList<AuditLogDto>>> GetAuditLogsByEntity(
        string tableName,
        string recordId)
    {
        var result = await _adminService.GetAuditLogsByEntityAsync(
            TenantId, tableName, recordId);
        return ToActionResult(result);
    }

    // GET /api/admin/subscriptions
    [HttpGet("subscriptions")]
    public async Task<ActionResult<IList<TenantSubscriptionDto>>> GetSubscriptions()
    {
        var result = await _adminService.GetSubscriptionHistoryAsync(TenantId);
        return ToActionResult(result);
    }

    // POST /api/admin/subscriptions
    [HttpPost("subscriptions")]
    public async Task<ActionResult<TenantSubscriptionDto>> CreateSubscription(
        [FromBody] TenantSubscriptionDto dto)
    {
        var result = await _adminService.CreateSubscriptionAsync(TenantId, dto);
        return ToActionResult(result);
    }
}
