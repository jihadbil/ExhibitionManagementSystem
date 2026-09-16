using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class InvoiceItemService : IInvoiceItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InvoiceItemService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<InvoiceItemDto>> GetItemByIdAsync(int tenantId, int itemId)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i => i.InvoiceItemID == itemId);

            if (item == null || item.Invoice.TenantID != tenantId)
            {
                return ServiceResult<InvoiceItemDto>.Failure("البند المحدد غير موجود", "INVOICE_ITEM_NOT_FOUND");
            }

            var dto = _mapper.Map<InvoiceItemDto>(item);
            return ServiceResult<InvoiceItemDto>.Success(dto);
        }

        public async Task<ServiceResult<IList<InvoiceItemDto>>> GetItemsByInvoiceIdAsync(int tenantId, int invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId);

            if (invoice == null)
            {
                return ServiceResult<IList<InvoiceItemDto>>.Failure("الفاتورة المحددة غير موجودة", "INVOICE_NOT_FOUND");
            }

            var items = await _unitOfWork.InvoiceItems.AsQueryable()
                .Where(i => i.InvoiceID == invoiceId)
                .ToListAsync();

            var dtos = _mapper.Map<IList<InvoiceItemDto>>(items);
            return ServiceResult<IList<InvoiceItemDto>>.Success(dtos);
        }

        public async Task<ServiceResult<InvoiceItemDto>> CreateItemAsync(int tenantId, int invoiceId, InvoiceItemCreateDto dto)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId);

            if (invoice == null)
            {
                return ServiceResult<InvoiceItemDto>.Failure("الفاتورة المحددة غير موجودة", "INVOICE_NOT_FOUND");
            }

            var item = _mapper.Map<InvoiceItem>(dto);
            item.InvoiceID = invoiceId;
            item.TotalPrice = item.Quantity * item.UnitPrice;

            await _unitOfWork.InvoiceItems.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();

            // إعادة احتساب إجمالي الفاتورة
            await RecalculateInvoiceTotalsAsync(invoiceId);

            var resultDto = _mapper.Map<InvoiceItemDto>(item);
            return ServiceResult<InvoiceItemDto>.Success(resultDto);
        }

        public async Task<ServiceResult<InvoiceItemDto>> UpdateItemAsync(int tenantId, int itemId, InvoiceItemUpdateDto dto)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i => i.InvoiceItemID == itemId);

            if (item == null || item.Invoice.TenantID != tenantId)
            {
                return ServiceResult<InvoiceItemDto>.Failure("البند المحدد غير موجود", "INVOICE_ITEM_NOT_FOUND");
            }

            _mapper.Map(dto, item);
            item.TotalPrice = item.Quantity * item.UnitPrice;

            _unitOfWork.InvoiceItems.Update(item);
            await _unitOfWork.SaveChangesAsync();

            // إعادة احتساب إجمالي الفاتورة
            await RecalculateInvoiceTotalsAsync(item.InvoiceID);

            var resultDto = _mapper.Map<InvoiceItemDto>(item);
            return ServiceResult<InvoiceItemDto>.Success(resultDto);
        }

        public async Task<ServiceResult<bool>> DeleteItemAsync(int tenantId, int itemId)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i => i.InvoiceItemID == itemId);

            if (item == null || item.Invoice.TenantID != tenantId)
            {
                return ServiceResult<bool>.Failure("البند المحدد غير موجود", "INVOICE_ITEM_NOT_FOUND");
            }

            var invoiceId = item.InvoiceID;

            _unitOfWork.InvoiceItems.Remove(item);
            await _unitOfWork.SaveChangesAsync();

            // إعادة احتساب إجمالي الفاتورة
            await RecalculateInvoiceTotalsAsync(invoiceId);

            return ServiceResult<bool>.Success(true);
        }

        private async Task RecalculateInvoiceTotalsAsync(int invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

            if (invoice != null)
            {
                invoice.SubTotal = invoice.InvoiceItems.Sum(item => item.TotalPrice);
                invoice.TaxAmount = invoice.SubTotal * (invoice.TaxRate / 100);
                invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;

                _unitOfWork.Invoices.Update(invoice);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
