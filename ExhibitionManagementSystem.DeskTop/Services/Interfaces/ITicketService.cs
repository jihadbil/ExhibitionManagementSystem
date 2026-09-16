using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface ITicketService
    {
        Task<ServiceResult<TicketDto>> IssueTicketAsync(int tenantId, TicketCreateDto dto);
        Task<ServiceResult<IList<TicketDto>>> GetByVisitorAsync(int tenantId, int visitorId);
        Task<ServiceResult<IList<TicketDto>>> GetByExhibitionAsync(int tenantId, int exhibitionId);
        Task<ServiceResult<TicketScanDto>> ScanTicketAsync(int tenantId, string qrCode, string direction, string? location, string scannedByUserId);
        Task<ServiceResult<IList<TicketScanDto>>> GetScanHistoryAsync(int tenantId, int ticketId);
        Task<ServiceResult> CancelTicketAsync(int tenantId, int ticketId);
    }
}
