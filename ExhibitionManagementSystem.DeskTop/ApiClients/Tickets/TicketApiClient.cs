using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Tickets;

public class TicketApiClient : ApiClientBase, ITicketService
{
    public TicketApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<TicketDto>> IssueTicketAsync(int tenantId, TicketCreateDto dto)
    {
        return PostAsync<TicketDto>("api/tickets", dto);
    }

    public Task<ServiceResult<IList<TicketDto>>> GetByVisitorAsync(int tenantId, int visitorId)
    {
        return GetAsync<IList<TicketDto>>($"api/tickets/visitor/{visitorId}");
    }

    public Task<ServiceResult<IList<TicketDto>>> GetByExhibitionAsync(int tenantId, int exhibitionId)
    {
        return GetAsync<IList<TicketDto>>($"api/tickets/exhibition/{exhibitionId}");
    }

    public Task<ServiceResult<TicketScanDto>> ScanTicketAsync(int tenantId, string qrCode, string direction, string? location, string scannedByUserId)
    {
        var request = new ScanTicketRequest(qrCode, direction, location);
        return PostAsync<TicketScanDto>("api/tickets/scan", request);
    }

    public Task<ServiceResult<IList<TicketScanDto>>> GetScanHistoryAsync(int tenantId, int ticketId)
    {
        return GetAsync<IList<TicketScanDto>>($"api/tickets/{ticketId}/scans");
    }

    public Task<ServiceResult> CancelTicketAsync(int tenantId, int ticketId)
    {
        return PatchVoidAsync($"api/tickets/{ticketId}/cancel");
    }
}

public record ScanTicketRequest(string QrCode, string Direction, string? Location);
