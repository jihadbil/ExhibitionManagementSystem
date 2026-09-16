using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IInvoiceItemService
    {
        Task<ServiceResult<InvoiceItemDto>> GetItemByIdAsync(int tenantId, int itemId);
        Task<ServiceResult<IList<InvoiceItemDto>>> GetItemsByInvoiceIdAsync(int tenantId, int invoiceId);
        Task<ServiceResult<InvoiceItemDto>> CreateItemAsync(int tenantId, int invoiceId, InvoiceItemCreateDto dto);
        Task<ServiceResult<InvoiceItemDto>> UpdateItemAsync(int tenantId, int itemId, InvoiceItemUpdateDto dto);
        Task<ServiceResult<bool>> DeleteItemAsync(int tenantId, int itemId);
    }
}
