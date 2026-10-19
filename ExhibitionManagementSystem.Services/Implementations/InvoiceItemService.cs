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

        public async Task<ServiceResult<InvoiceItemDto>> GetItemByIdAsync(int tenantId, int itemId)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice)
                .FirstOrDefaultAsync(i => i.InvoiceItemID == itemId, cancellationToken: _unitOfWork.RequestCancellation);

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
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

            if (invoice == null)
            {
                return ServiceResult<IList<InvoiceItemDto>>.Failure("الفاتورة المحددة غير موجودة", "INVOICE_NOT_FOUND");
            }

            var items = await _unitOfWork.InvoiceItems.AsQueryable()
                .Where(i => i.InvoiceID == invoiceId)
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var dtos = _mapper.Map<IList<InvoiceItemDto>>(items);
            return ServiceResult<IList<InvoiceItemDto>>.Success(dtos);
        }

        public async Task<ServiceResult<InvoiceItemDto>> CreateItemAsync(int tenantId, int invoiceId, InvoiceItemCreateDto dto)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.InvoiceItems).Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

            if (invoice == null)
            {
                return ServiceResult<InvoiceItemDto>.Failure("الفاتورة المحددة غير موجودة", "INVOICE_NOT_FOUND");
            }

            if (!CanEdit(invoice))
                return ServiceResult<InvoiceItemDto>.Failure("لا يمكن تعديل فاتورة مسددة أو ملغاة", "INVOICE_LOCKED");
            if (!ValidLine(dto.Quantity, dto.UnitPrice))
                return ServiceResult<InvoiceItemDto>.Failure("الكمية والسعر غير صالحين", "INVALID_INVOICE_ITEM");

            var item = _mapper.Map<InvoiceItem>(dto);
            item.InvoiceID = invoiceId;
            item.TotalPrice = decimal.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);

            invoice.InvoiceItems.Add(item);
            await RecalculateInvoiceTotalsAsync(invoice);

            var resultDto = _mapper.Map<InvoiceItemDto>(item);
            return ServiceResult<InvoiceItemDto>.Success(resultDto);
        }

        public async Task<ServiceResult<InvoiceItemDto>> UpdateItemAsync(int tenantId, int itemId, InvoiceItemUpdateDto dto)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice).ThenInclude(i => i.InvoiceItems)
                .Include(i => i.Invoice).ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceItemID == itemId, cancellationToken: _unitOfWork.RequestCancellation);

            if (item == null || item.Invoice.TenantID != tenantId)
            {
                return ServiceResult<InvoiceItemDto>.Failure("البند المحدد غير موجود", "INVOICE_ITEM_NOT_FOUND");
            }

            if (!CanEdit(item.Invoice))
                return ServiceResult<InvoiceItemDto>.Failure("لا يمكن تعديل فاتورة مسددة أو ملغاة", "INVOICE_LOCKED");
            if (!ValidLine(dto.Quantity, dto.UnitPrice))
                return ServiceResult<InvoiceItemDto>.Failure("الكمية والسعر غير صالحين", "INVALID_INVOICE_ITEM");

            item.ItemName = dto.ItemName;
            item.Quantity = dto.Quantity;
            item.UnitPrice = dto.UnitPrice;
            item.TotalPrice = decimal.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);

            _unitOfWork.InvoiceItems.Update(item);
            await RecalculateInvoiceTotalsAsync(item.Invoice);

            var resultDto = _mapper.Map<InvoiceItemDto>(item);
            return ServiceResult<InvoiceItemDto>.Success(resultDto);
        }

        public async Task<ServiceResult<bool>> DeleteItemAsync(int tenantId, int itemId)
        {
            var item = await _unitOfWork.InvoiceItems.AsQueryable()
                .Include(i => i.Invoice).ThenInclude(i => i.InvoiceItems)
                .Include(i => i.Invoice).ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceItemID == itemId, cancellationToken: _unitOfWork.RequestCancellation);

            if (item == null || item.Invoice.TenantID != tenantId)
            {
                return ServiceResult<bool>.Failure("البند المحدد غير موجود", "INVOICE_ITEM_NOT_FOUND");
            }

            if (!CanEdit(item.Invoice))
                return ServiceResult<bool>.Failure("لا يمكن تعديل فاتورة مسددة أو ملغاة", "INVOICE_LOCKED");

            item.Invoice.InvoiceItems.Remove(item);
            _unitOfWork.InvoiceItems.Remove(item);
            await RecalculateInvoiceTotalsAsync(item.Invoice);

            return ServiceResult<bool>.Success(true);
        }

        private static bool CanEdit(Invoice invoice) =>
            invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Issued
            && !invoice.Payments.Any(p => p.Status == PaymentStatus.Completed);

        private static bool ValidLine(decimal quantity, decimal price) => quantity is > 0 and <= 99999999.99m
            && price is >= 0 and <= 9999999999999999.99m && decimal.Round(quantity, 2) == quantity
            && decimal.Round(price, 2) == price && quantity * price <= 9999999999999999.99m;

        private async Task RecalculateInvoiceTotalsAsync(Invoice invoice)
        {
            invoice.SubTotal = decimal.Round(invoice.InvoiceItems.Sum(item => item.TotalPrice), 2);
            invoice.TaxAmount = decimal.Round(invoice.SubTotal * invoice.TaxRate / 100, 2, MidpointRounding.AwayFromZero);
            invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
            if (invoice.TotalAmount > 9999999999999999.99m)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Invoice total exceeds the supported amount.");
            _unitOfWork.Invoices.Update(invoice);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
