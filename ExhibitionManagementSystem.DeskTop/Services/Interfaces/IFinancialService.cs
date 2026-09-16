using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IFinancialService
    {
        Task<ServiceResult<PagedResultDto<InvoiceDto>>> GetInvoicesByTenantAsync(int tenantId, int page, int pageSize);
        Task<ServiceResult<InvoiceDto>> GetInvoiceByIdAsync(int tenantId, int invoiceId);
        Task<ServiceResult<InvoiceDto>> GetInvoiceByReservationAsync(int tenantId, int reservationId);
        Task<ServiceResult<IList<InvoiceDto>>> GetOverdueInvoicesAsync(int tenantId);
        Task<ServiceResult<InvoiceDto>> GenerateInvoiceForReservationAsync(int tenantId, int reservationId);
        Task<ServiceResult<InvoiceDto>> CreateInvoiceAsync(int tenantId, InvoiceCreateDto dto);
        Task<ServiceResult<PaymentDto>> RecordPaymentAsync(int tenantId, string userId, PaymentCreateDto dto);
        Task<ServiceResult<IList<PaymentDto>>> GetPaymentsByInvoiceAsync(int tenantId, int invoiceId);
    }
}
