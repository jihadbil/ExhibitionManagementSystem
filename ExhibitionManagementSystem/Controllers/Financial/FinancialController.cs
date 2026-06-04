using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Financial;

[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class FinancialController : BaseApiController
{
    private readonly IFinancialService _financialService;

    public FinancialController(IFinancialService financialService)
        => _financialService = financialService;

    // ─── Invoices ─────────────────────────────────────────────────────────────

    // GET /api/financial/invoices?page=1&pageSize=20
    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResultDto<InvoiceDto>>> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _financialService.GetInvoicesByTenantAsync(
            TenantId, page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/financial/invoices/overdue
    [HttpGet("invoices/overdue")]
    public async Task<ActionResult<IList<InvoiceDto>>> GetOverdueInvoices()
    {
        var result = await _financialService.GetOverdueInvoicesAsync(TenantId);
        return ToActionResult(result);
    }

    // GET /api/financial/invoices/{invoiceId}
    [HttpGet("invoices/{invoiceId:int}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceById(int invoiceId)
    {
        var result = await _financialService.GetInvoiceByIdAsync(TenantId, invoiceId);
        return ToActionResult(result);
    }

    // GET /api/financial/invoices/by-reservation/{reservationId}
    [HttpGet("invoices/by-reservation/{reservationId:int}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceByReservation(
        int reservationId)
    {
        var result = await _financialService.GetInvoiceByReservationAsync(
            TenantId, reservationId);
        return ToActionResult(result);
    }

    // POST /api/financial/invoices
    [HttpPost("invoices")]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(
        [FromBody] InvoiceCreateDto dto)
    {
        var result = await _financialService.CreateInvoiceAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetInvoiceById),
            new { invoiceId = result.Data!.InvoiceID }, result.Data);
    }

    // POST /api/financial/invoices/generate/{reservationId}
    [HttpPost("invoices/generate/{reservationId:int}")]
    public async Task<ActionResult<InvoiceDto>> GenerateInvoice(int reservationId)
    {
        var result = await _financialService.GenerateInvoiceForReservationAsync(
            TenantId, reservationId);
        return ToActionResult(result);
    }

    // ─── Payments ─────────────────────────────────────────────────────────────

    // POST /api/financial/payments
    [HttpPost("payments")]
    public async Task<ActionResult<PaymentDto>> RecordPayment(
        [FromBody] PaymentCreateDto dto)
    {
        // UserId يُستخرج من JWT — لا يُؤخذ من الـ Body
        var result = await _financialService.RecordPaymentAsync(TenantId, UserId, dto);
        return ToActionResult(result);
    }

    // GET /api/financial/payments/invoice/{invoiceId}
    [HttpGet("payments/invoice/{invoiceId:int}")]
    public async Task<ActionResult<IList<PaymentDto>>> GetPaymentsByInvoice(
        int invoiceId)
    {
        var result = await _financialService.GetPaymentsByInvoiceAsync(
            TenantId, invoiceId);
        return ToActionResult(result);
    }
}
