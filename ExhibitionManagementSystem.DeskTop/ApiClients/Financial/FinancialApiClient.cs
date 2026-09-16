using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Financial;

public class FinancialApiClient : ApiClientBase, IFinancialService
{
    public FinancialApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<PagedResultDto<InvoiceDto>>> GetInvoicesByTenantAsync(int tenantId, int page, int pageSize)
    {
        return GetAsync<PagedResultDto<InvoiceDto>>($"api/financial/invoices?page={page}&pageSize={pageSize}");
    }

    public Task<ServiceResult<InvoiceDto>> GetInvoiceByIdAsync(int tenantId, int invoiceId)
    {
        return GetAsync<InvoiceDto>($"api/financial/invoices/{invoiceId}");
    }

    public Task<ServiceResult<InvoiceDto>> GetInvoiceByReservationAsync(int tenantId, int reservationId)
    {
        return GetAsync<InvoiceDto>($"api/financial/invoices/by-reservation/{reservationId}");
    }

    public Task<ServiceResult<IList<InvoiceDto>>> GetOverdueInvoicesAsync(int tenantId)
    {
        return GetAsync<IList<InvoiceDto>>("api/financial/invoices/overdue");
    }

    public Task<ServiceResult<InvoiceDto>> GenerateInvoiceForReservationAsync(int tenantId, int reservationId)
    {
        return PostAsync<InvoiceDto>($"api/financial/invoices/generate/{reservationId}", new { });
    }

    public Task<ServiceResult<InvoiceDto>> CreateInvoiceAsync(int tenantId, InvoiceCreateDto dto)
    {
        return PostAsync<InvoiceDto>("api/financial/invoices", dto);
    }

    public Task<ServiceResult<PaymentDto>> RecordPaymentAsync(int tenantId, string userId, PaymentCreateDto dto)
    {
        return PostAsync<PaymentDto>("api/financial/payments", dto);
    }

    public Task<ServiceResult<IList<PaymentDto>>> GetPaymentsByInvoiceAsync(int tenantId, int invoiceId)
    {
        return GetAsync<IList<PaymentDto>>($"api/financial/payments/invoice/{invoiceId}");
    }
}
