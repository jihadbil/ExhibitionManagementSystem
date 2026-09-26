using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Financial
{
    public class InvoiceItemApiClient : ApiClientBase, IInvoiceItemService
    {
        public InvoiceItemApiClient(IHttpClientFactory httpClientFactory, SessionService session)
            : base(httpClientFactory, session)
        {
        }

        public Task<ServiceResult<InvoiceItemDto>> GetItemByIdAsync(int tenantId, int invoiceId, int itemId)
        {
            return GetAsync<InvoiceItemDto>($"api/financial/invoices/{invoiceId}/items/{itemId}");
        }

        public Task<ServiceResult<IList<InvoiceItemDto>>> GetItemsByInvoiceIdAsync(int tenantId, int invoiceId)
        {
            return GetAsync<IList<InvoiceItemDto>>($"api/financial/invoices/{invoiceId}/items");
        }

        public Task<ServiceResult<InvoiceItemDto>> CreateItemAsync(int tenantId, int invoiceId, InvoiceItemCreateDto dto)
        {
            return PostAsync<InvoiceItemDto>($"api/financial/invoices/{invoiceId}/items", dto);
        }

        public Task<ServiceResult<InvoiceItemDto>> UpdateItemAsync(int tenantId, int invoiceId, int itemId, InvoiceItemUpdateDto dto)
        {
            return PutAsync<InvoiceItemDto>($"api/financial/invoices/{invoiceId}/items/{itemId}", dto);
        }

        public async Task<ServiceResult<bool>> DeleteItemAsync(int tenantId, int invoiceId, int itemId)
        {
            var result = await DeleteAsync($"api/financial/invoices/{invoiceId}/items/{itemId}");
            if (result.IsSuccess)
            {
                return ServiceResult<bool>.Success(true);
            }
            return ServiceResult<bool>.Failure(result.ErrorMessage ?? string.Empty, result.ErrorCode);
        }
    }
}
