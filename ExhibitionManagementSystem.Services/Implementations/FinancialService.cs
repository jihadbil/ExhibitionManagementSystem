using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class FinancialService : IFinancialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public FinancialService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<ServiceResult<PagedResultDto<InvoiceDto>>> GetInvoicesByTenantAsync(int tenantId, int page, int pageSize)
        {
            var query = _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .AsSplitQuery()
                .Where(i => i.TenantID == tenantId);

            var totalCount = await query.CountAsync(cancellationToken: _unitOfWork.RequestCancellation);
            var items = await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var dtos = _mapper.Map<IList<InvoiceDto>>(items);
            var result = new PagedResultDto<InvoiceDto>
            {
                Items = dtos.ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<InvoiceDto>>.Success(result);
        }

        public async Task<ServiceResult<InvoiceDto>> GetInvoiceByIdAsync(int tenantId, int invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                    .ThenInclude(p => p.ReceivedByUser)
                .Include(i => i.InvoiceItems)
                .AsSplitQuery()
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

            if (invoice == null)
            {
                return ServiceResult<InvoiceDto>.Failure("الفاتورة غير موجودة", "INVOICE_NOT_FOUND");
            }

            var dto = _mapper.Map<InvoiceDto>(invoice);
            return ServiceResult<InvoiceDto>.Success(dto);
        }

        public async Task<ServiceResult<InvoiceDto>> GetInvoiceByReservationAsync(int tenantId, int reservationId)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .AsSplitQuery()
                .FirstOrDefaultAsync(i => i.ReservationID == reservationId && i.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

            if (invoice == null)
            {
                return ServiceResult<InvoiceDto>.Failure("الفاتورة غير موجودة لهذا الحجز", "INVOICE_NOT_FOUND");
            }

            var dto = _mapper.Map<InvoiceDto>(invoice);
            return ServiceResult<InvoiceDto>.Success(dto);
        }

        public async Task<ServiceResult<IList<InvoiceDto>>> GetOverdueInvoicesAsync(int tenantId)
        {
            var overdue = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .AsSplitQuery()
                .Where(i => i.TenantID == tenantId && 
                            i.Status != InvoiceStatus.Paid && 
                            i.Status != InvoiceStatus.Cancelled && 
                            i.DueDate < DateTime.UtcNow)
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var dtos = _mapper.Map<IList<InvoiceDto>>(overdue);
            return ServiceResult<IList<InvoiceDto>>.Success(dtos);
        }

        public async Task<ServiceResult<InvoiceDto>> GenerateInvoiceForReservationAsync(int tenantId, int reservationId)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.Booth)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<InvoiceDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            var existing = await _unitOfWork.Invoices.GetByReservationAsync(reservationId);
            if (existing != null)
            {
                return ServiceResult<InvoiceDto>.Failure("تم إنشاء فاتورة مسبقاً لهذا الحجز", "INVOICE_ALREADY_EXISTS");
            }

            string invoiceNumber = await _unitOfWork.Invoices.GenerateNextInvoiceNumberAsync(tenantId);
            decimal taxRate = decimal.TryParse(_configuration["Financial:DefaultTaxRate"], out var cfgRate)
                ? cfgRate : 0.0m;
            if (taxRate < 0 || taxRate > 100)
                return ServiceResult<InvoiceDto>.Failure("إعداد الضريبة غير صالح", "INVALID_TAX_CONFIGURATION");
            decimal subTotal = reservation.TotalAmount;
            decimal taxAmount = decimal.Round(subTotal * taxRate / 100, 2, MidpointRounding.AwayFromZero);
            decimal totalAmount = subTotal + taxAmount;

            var invoice = new Invoice
            {
                TenantID = tenantId,
                ReservationID = reservationId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateTime.UtcNow,
                SubTotal = subTotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                CurrencyCode = reservation.CurrencyCode,
                Status = reservation.Status == ReservationStatus.Confirmed 
                    ? InvoiceStatus.Issued 
                    : InvoiceStatus.Draft,
                DueDate = DateTime.UtcNow.AddDays(30), // Default due date is 30 days from now
                Notes = $"Invoice for booth reservation #{reservationId}",
                CreatedAt = DateTime.UtcNow
            };

            // 1. توليد بند الجناح (Booth Rental Item)
            var boothItemName = reservation.Booth != null 
                ? $"حجز جناح - رقم {reservation.Booth.BoothNumber} ({reservation.BoothTypeSelected})"
                : $"حجز جناح - نوع {reservation.BoothTypeSelected}";

            var boothItem = new InvoiceItem
            {
                ItemName = boothItemName,
                Quantity = 1,
                UnitPrice = reservation.BoothAmount,
                TotalPrice = reservation.BoothAmount
            };
            invoice.InvoiceItems.Add(boothItem);

            // 2. توليد بنود الخدمات الإضافية (Services Items)
            if (reservation.ReservationServices != null)
            {
                foreach (var rs in reservation.ReservationServices)
                {
                    var serviceName = rs.Service != null ? rs.Service.ServiceName : "خدمة إضافية";
                    var serviceItem = new InvoiceItem
                    {
                        ItemName = serviceName,
                        Quantity = rs.Quantity,
                        UnitPrice = rs.UnitPrice,
                        TotalPrice = rs.TotalPrice
                    };
                    invoice.InvoiceItems.Add(serviceItem);
                }
            }

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            var fullInvoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .AsSplitQuery()
                .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID, cancellationToken: _unitOfWork.RequestCancellation);

            var dto = _mapper.Map<InvoiceDto>(fullInvoice ?? invoice);
            return ServiceResult<InvoiceDto>.Success(dto);
        }

        public async Task<ServiceResult<InvoiceDto>> CreateInvoiceAsync(int tenantId, InvoiceCreateDto dto)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .FirstOrDefaultAsync(r => r.ReservationID == dto.ReservationID, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<InvoiceDto>.Failure("الحجز المحدد غير موجود", "RESERVATION_NOT_FOUND");
            }

            var existing = await _unitOfWork.Invoices.GetByReservationAsync(dto.ReservationID);
            if (existing != null)
            {
                return ServiceResult<InvoiceDto>.Failure("تم إنشاء فاتورة مسبقاً لهذا الحجز", "INVOICE_ALREADY_EXISTS");
            }

            var invoice = _mapper.Map<Invoice>(dto);
            if (dto.TaxRate < 0 || dto.TaxRate > 100
                || !string.Equals(dto.CurrencyCode, reservation.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                return ServiceResult<InvoiceDto>.Failure("الضريبة أو عملة الفاتورة غير صالحة", "INVALID_INVOICE");
            invoice.SubTotal = reservation.TotalAmount;
            invoice.TaxAmount = decimal.Round(invoice.SubTotal * dto.TaxRate / 100, 2, MidpointRounding.AwayFromZero);
            invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
            invoice.InvoiceItems.Add(new InvoiceItem { ItemName = "حجز أجنحة وخدمات", Quantity = 1,
                UnitPrice = invoice.SubTotal, TotalPrice = invoice.SubTotal });
            invoice.TenantID = tenantId;
            invoice.Status = InvoiceStatus.Issued;
            invoice.InvoiceDate = DateTime.UtcNow;

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            var fullInvoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .AsSplitQuery()
                .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID, cancellationToken: _unitOfWork.RequestCancellation);

            var resultDto = _mapper.Map<InvoiceDto>(fullInvoice ?? invoice);
            return ServiceResult<InvoiceDto>.Success(resultDto);
        }

        public async Task<ServiceResult<PaymentDto>> RecordPaymentAsync(int tenantId, string userId, PaymentCreateDto dto)
        {
            if (dto.Amount <= 0 || decimal.Round(dto.Amount, 2) != dto.Amount)
                return ServiceResult<PaymentDto>.Failure("قيمة الدفعة يجب أن تكون موجبة وبدقة منزلتين عشريتين", "INVALID_PAYMENT_AMOUNT");
            if (string.IsNullOrEmpty(dto.IdempotencyKey) || dto.IdempotencyKey.Length > 100
                || !System.Text.RegularExpressions.Regex.IsMatch(dto.IdempotencyKey, "^[A-Za-z0-9._:-]+$"))
                return ServiceResult<PaymentDto>.Failure("مفتاح إعادة الطلب مطلوب", "INVALID_IDEMPOTENCY_KEY");
            if (!Enum.TryParse<PaymentMethod>(dto.Method, true, out var method) || !Enum.IsDefined(method))
                return ServiceResult<PaymentDto>.Failure("طريقة الدفع غير صالحة", "INVALID_PAYMENT_METHOD");
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceID == dto.InvoiceID && i.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

            if (invoice == null)
            {
                return ServiceResult<PaymentDto>.Failure("الفاتورة غير موجودة", "INVOICE_NOT_FOUND");
            }

            var previous = await FindRecordedPaymentAsync(tenantId, dto.InvoiceID, dto.IdempotencyKey);
            if (previous != null) return ReplayPayment(previous, dto, method);

            if (invoice.Status == InvoiceStatus.Paid)
            {
                return ServiceResult<PaymentDto>.Failure("الفاتورة مدفوعة بالكامل بالفعل", "INVOICE_ALREADY_PAID");
            }

            if (invoice.Status == InvoiceStatus.Cancelled || invoice.Status == InvoiceStatus.Draft)
            {
                return ServiceResult<PaymentDto>.Failure("لا يمكن سداد دفعة لفاتورة ملغاة", "INVOICE_CANCELLED");
            }

            if (!string.Equals(invoice.CurrencyCode, dto.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                return ServiceResult<PaymentDto>.Failure("عملة الدفعة يجب أن تطابق عملة الفاتورة", "PAYMENT_CURRENCY_MISMATCH");

            decimal totalPaidBefore = invoice.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            decimal totalPaidAfter = totalPaidBefore + dto.Amount;

            if (totalPaidAfter > invoice.TotalAmount)
            {
                return ServiceResult<PaymentDto>.Failure("قيمة الدفعة تتجاوز القيمة المتبقية للفاتورة", "PAYMENT_AMOUNT_EXCEEDS_REMAINING");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = new Payment
                {
                    InvoiceID = dto.InvoiceID,
                    IdempotencyKey = dto.IdempotencyKey,
                    PaymentDate = DateTime.UtcNow,
                    Amount = dto.Amount,
                    CurrencyCode = invoice.CurrencyCode,
                    Method = method,
                    ReferenceNo = dto.ReferenceNo ?? string.Empty,
                    Status = PaymentStatus.Completed,
                    Notes = dto.Notes ?? string.Empty,
                    ReceivedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Payments.AddAsync(payment);

                if (totalPaidAfter >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.Paid;
                }
                else
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                invoice.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Invoices.Update(invoice);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var fullPayment = await _unitOfWork.Payments.AsQueryable()
                    .Include(p => p.Invoice)
                    .Include(p => p.Currency)
                    .Include(p => p.ReceivedByUser)
                    .FirstOrDefaultAsync(p => p.PaymentID == payment.PaymentID, cancellationToken: _unitOfWork.RequestCancellation);

                var resultDto = _mapper.Map<PaymentDto>(fullPayment ?? payment);
                return ServiceResult<PaymentDto>.Success(resultDto);
            }
            catch (Exception ex) when (ExhibitionManagementSystem.DataAccess.DatabaseConflicts.IsConflict(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                previous = await FindRecordedPaymentAsync(tenantId, dto.InvoiceID, dto.IdempotencyKey);
                return previous != null ? ReplayPayment(previous, dto, method)
                    : ServiceResult<PaymentDto>.Failure("تعارض التحديث؛ أعد الطلب بالمفتاح نفسه", "CONFLICT");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private Task<Payment?> FindRecordedPaymentAsync(int tenantId, int invoiceId, string key) =>
            _unitOfWork.Payments.AsQueryable().AsNoTracking().Include(p => p.Invoice)
                .Include(p => p.Currency).Include(p => p.ReceivedByUser)
                .SingleOrDefaultAsync(p => p.InvoiceID == invoiceId && p.Invoice.TenantID == tenantId && p.IdempotencyKey == key, cancellationToken: _unitOfWork.RequestCancellation);

        private ServiceResult<PaymentDto> ReplayPayment(Payment payment, PaymentCreateDto dto, PaymentMethod method) =>
            payment.Amount == dto.Amount && payment.Method == method
                && string.Equals(payment.CurrencyCode, dto.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                && payment.ReferenceNo == (dto.ReferenceNo ?? string.Empty) && payment.Notes == (dto.Notes ?? string.Empty)
            ? ServiceResult<PaymentDto>.Success(_mapper.Map<PaymentDto>(payment))
            : ServiceResult<PaymentDto>.Failure("المفتاح مستخدم لطلب مختلف", "IDEMPOTENCY_CONFLICT");

        public async Task<ServiceResult<IList<PaymentDto>>> GetPaymentsByInvoiceAsync(int tenantId, int invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
            if (invoice == null || invoice.TenantID != tenantId)
            {
                return ServiceResult<IList<PaymentDto>>.Failure("الفاتورة غير موجودة", "INVOICE_NOT_FOUND");
            }

            var payments = await _unitOfWork.Payments.AsQueryable()
                .Include(p => p.Invoice)
                .Include(p => p.Currency)
                .Include(p => p.ReceivedByUser)
                .Where(p => p.InvoiceID == invoiceId)
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var dtos = _mapper.Map<IList<PaymentDto>>(payments);
            return ServiceResult<IList<PaymentDto>>.Success(dtos);
        }
    }
}
