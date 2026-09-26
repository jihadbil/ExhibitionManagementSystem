using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
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

        public async Task<ServiceResult<InvoiceItemDto>> GetItemByIdAsync(int tenantId, int invoiceId, int itemId)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i =>
                    i.InvoiceItemID == itemId &&
                    i.InvoiceID == invoiceId &&
                    i.Invoice.TenantID == tenantId);

            if (item == null)
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

            if (invoice.Status != InvoiceStatus.Draft)
            {
                return ServiceResult<InvoiceItemDto>.Failure(
                    "لا يمكن تعديل بنود الفاتورة بعد إصدارها",
                    "INVOICE_IMMUTABLE");
            }

            var item = _mapper.Map<InvoiceItem>(dto);
            item.InvoiceID = invoiceId;
            item.TotalPrice = item.Quantity * item.UnitPrice;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.InvoiceItems.AddAsync(item);
                await _unitOfWork.SaveChangesAsync();
                await RecalculateInvoiceTotalsAsync(tenantId, invoiceId);
                await _unitOfWork.CommitTransactionAsync();

                var resultDto = _mapper.Map<InvoiceItemDto>(item);
                return ServiceResult<InvoiceItemDto>.Success(resultDto);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult<InvoiceItemDto>> UpdateItemAsync(int tenantId, int invoiceId, int itemId, InvoiceItemUpdateDto dto)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i =>
                    i.InvoiceItemID == itemId &&
                    i.InvoiceID == invoiceId &&
                    i.Invoice.TenantID == tenantId);

            if (item == null)
            {
                return ServiceResult<InvoiceItemDto>.Failure("البند المحدد غير موجود", "INVOICE_ITEM_NOT_FOUND");
            }

            if (item.Invoice.Status != InvoiceStatus.Draft)
            {
                return ServiceResult<InvoiceItemDto>.Failure(
                    "لا يمكن تعديل بنود الفاتورة بعد إصدارها",
                    "INVOICE_IMMUTABLE");
            }

            _mapper.Map(dto, item);
            item.TotalPrice = item.Quantity * item.UnitPrice;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.InvoiceItems.Update(item);
                await _unitOfWork.SaveChangesAsync();
                await RecalculateInvoiceTotalsAsync(tenantId, invoiceId);
                await _unitOfWork.CommitTransactionAsync();

                var resultDto = _mapper.Map<InvoiceItemDto>(item);
                return ServiceResult<InvoiceItemDto>.Success(resultDto);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult<bool>> DeleteItemAsync(int tenantId, int invoiceId, int itemId)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i =>
                    i.InvoiceItemID == itemId &&
                    i.InvoiceID == invoiceId &&
                    i.Invoice.TenantID == tenantId);

            if (item == null)
            {
                return ServiceResult<bool>.Failure("البند المحدد غير موجود", "INVOICE_ITEM_NOT_FOUND");
            }

            if (item.Invoice.Status != InvoiceStatus.Draft)
            {
                return ServiceResult<bool>.Failure(
                    "لا يمكن تعديل بنود الفاتورة بعد إصدارها",
                    "INVOICE_IMMUTABLE");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.InvoiceItems.Remove(item);
                await _unitOfWork.SaveChangesAsync();
                await RecalculateInvoiceTotalsAsync(tenantId, invoiceId);
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task RecalculateInvoiceTotalsAsync(int tenantId, int invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId);

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
