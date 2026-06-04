using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Reports;

[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
        => _reportService = reportService;

    // POST /api/reports/exhibitions/{exhibitionId}
    // يُولّد تقريراً مالياً كاملاً ويحفظه في قاعدة البيانات
    [HttpPost("exhibitions/{exhibitionId:int}")]
    public async Task<ActionResult<FinancialReportDto>> Generate(int exhibitionId)
    {
        var result = await _reportService.GenerateExhibitionReportAsync(
            TenantId, exhibitionId, UserId);
        return ToActionResult(result);
    }

    // GET /api/reports/{reportId}
    [HttpGet("{reportId:int}")]
    public async Task<ActionResult<FinancialReportDto>> GetById(int reportId)
    {
        var result = await _reportService.GetReportByIdAsync(TenantId, reportId);
        return ToActionResult(result);
    }
}
