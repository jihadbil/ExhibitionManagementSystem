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
            if (!TryCalculateTotal(dto.Quantity, dto.UnitPrice, out var totalPrice))
                return ServiceResult<InvoiceItemDto>.Failure("كمية البند أو سعره أو إجماليه يتجاوز الدقة أو القيمة المسموحة", "INVALID_INVOICE_ITEM_VALUES");
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
            item.TotalPrice = totalPrice;

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
            if (!TryCalculateTotal(dto.Quantity, dto.UnitPrice, out var totalPrice))
                return ServiceResult<InvoiceItemDto>.Failure("كمية البند أو سعره أو إجماليه يتجاوز الدقة أو القيمة المسموحة", "INVALID_INVOICE_ITEM_VALUES");
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
            item.TotalPrice = totalPrice;

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

        private static bool TryCalculateTotal(decimal quantity, decimal unitPrice, out decimal total)
        {
            total = 0;
            if (quantity <= 0 || quantity > 9999999999999999.99m || decimal.Round(quantity, 2) != quantity ||
                unitPrice < 0 || unitPrice > 99999999999999999999.99999999m || decimal.Round(unitPrice, 8) != unitPrice)
                return false;
            try
            {
                total = InvoiceFinancialSummary.Round(quantity * unitPrice);
                return total <= 9999999999999999.99m;
            }
            catch (OverflowException) { return false; }
        }

        private async Task RecalculateInvoiceTotalsAsync(int tenantId, int invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId);

            if (invoice != null)
            {
                invoice.SubTotal = invoice.InvoiceItems.Sum(item => item.TotalPrice);
                invoice.TaxAmount = InvoiceFinancialSummary.Round(invoice.SubTotal * (invoice.TaxRate / 100));
                invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
                invoice.TotalAmountInBaseCurrency = decimal.Round(
                    invoice.TotalAmount * invoice.ExchangeRateUsed, 2, MidpointRounding.AwayFromZero);

                _unitOfWork.Invoices.Update(invoice);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
