using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Visitor;

[Route("api/[controller]")]
public class TicketsController : BaseApiController
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
        => _ticketService = ticketService;

    // POST /api/tickets
    [HttpPost]
    public async Task<ActionResult<TicketDto>> Issue(
        [FromBody] TicketCreateDto dto)
    {
        var result = await _ticketService.IssueTicketAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetScanHistory),
            new { ticketId = result.Data!.TicketID }, result.Data);
    }

    // GET /api/tickets/visitor/{visitorId}
    [HttpGet("visitor/{visitorId:int}")]
    public async Task<ActionResult<IList<TicketDto>>> GetByVisitor(int visitorId)
    {
        var result = await _ticketService.GetByVisitorAsync(TenantId, visitorId);
        return ToActionResult(result);
    }

    // GET /api/tickets/exhibition/{exhibitionId}
    [HttpGet("exhibition/{exhibitionId:int}")]
    public async Task<ActionResult<IList<TicketDto>>> GetByExhibition(int exhibitionId)
    {
        var result = await _ticketService.GetByExhibitionAsync(TenantId, exhibitionId);
        return ToActionResult(result);
    }

    // POST /api/tickets/scan
    // Body: { "qrCode": "3-42-abc123", "direction": "In", "location": "Gate A" }
    [HttpPost("scan")]
    public async Task<ActionResult<TicketScanDto>> Scan(
        [FromBody] ScanTicketRequest request)
    {
        // UserId يُستخرج من JWT تلقائياً — لا يُؤخذ من الـ Body
        var result = await _ticketService.ScanTicketAsync(
            TenantId,
            request.QrCode,
            request.Direction,
            request.Location,
            UserId);
        return ToActionResult(result);
    }

    // GET /api/tickets/{ticketId}/scans
    [HttpGet("{ticketId:int}/scans")]
    public async Task<ActionResult<IList<TicketScanDto>>> GetScanHistory(int ticketId)
    {
        var result = await _ticketService.GetScanHistoryAsync(TenantId, ticketId);
        return ToActionResult(result);
    }
}

public record ScanTicketRequest(string QrCode, string Direction, string? Location);
