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

        public Task<ServiceResult<InvoiceItemDto>> GetItemByIdAsync(int tenantId, int itemId)
        {
            // نمرر الـ invoiceId بقيمة افتراضية 0 لأن المعرّف الفريد للبند يكفي للبحث في الـ API
            return GetAsync<InvoiceItemDto>($"api/financial/invoices/0/items/{itemId}");
        }

        public Task<ServiceResult<IList<InvoiceItemDto>>> GetItemsByInvoiceIdAsync(int tenantId, int invoiceId)
        {
            return GetAsync<IList<InvoiceItemDto>>($"api/financial/invoices/{invoiceId}/items");
        }

        public Task<ServiceResult<InvoiceItemDto>> CreateItemAsync(int tenantId, int invoiceId, InvoiceItemCreateDto dto)
        {
            return PostAsync<InvoiceItemDto>($"api/financial/invoices/{invoiceId}/items", dto);
        }

        public Task<ServiceResult<InvoiceItemDto>> UpdateItemAsync(int tenantId, int itemId, InvoiceItemUpdateDto dto)
        {
            // نرسل الطلب لتحديث البند التابع للفاتورة
            return PutAsync<InvoiceItemDto>($"api/financial/invoices/{dto.InvoiceItemID}/items/{itemId}", dto);
        }

        public async Task<ServiceResult<bool>> DeleteItemAsync(int tenantId, int itemId)
        {
            var result = await DeleteAsync($"api/financial/invoices/0/items/{itemId}");
            if (result.IsSuccess)
            {
                return ServiceResult<bool>.Success(true);
            }
            return ServiceResult<bool>.Failure(result.ErrorMessage ?? string.Empty, result.ErrorCode);
        }
    }
}

