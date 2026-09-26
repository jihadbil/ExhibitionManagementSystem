using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Financial;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class FinancialService : IFinancialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public FinancialService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IConfiguration configuration,
            ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<ServiceResult<PagedResultDto<InvoiceDto>>> GetInvoicesByTenantAsync(int tenantId, int page, int pageSize)
        {
            var query = _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .Include(i => i.CreditNotes)
                .Where(i => i.TenantID == tenantId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
                .Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantID == tenantId);

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
                .Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.ReservationID == reservationId && i.TenantID == tenantId);

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
                .Include(i => i.CreditNotes)
                .Where(i => i.TenantID == tenantId && 
                            i.Status != InvoiceStatus.Paid && 
                            i.Status != InvoiceStatus.Cancelled && 
                            i.DueDate < DateTime.UtcNow)
                .ToListAsync();

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
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

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
            decimal subTotal = reservation.TotalAmount;
            decimal taxAmount = subTotal * (taxRate / 100);
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

            try
            {
                await _unitOfWork.Invoices.AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ServiceResult<InvoiceDto>.Failure(
                    "تعارض إصدار الفاتورة مع عملية متزامنة. أعد تحميل البيانات.",
                    "INVOICE_NUMBER_CONFLICT");
            }

            var fullInvoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);

            var dto = _mapper.Map<InvoiceDto>(fullInvoice ?? invoice);
            return ServiceResult<InvoiceDto>.Success(dto);
        }

        public async Task<ServiceResult<InvoiceDto>> CreateInvoiceAsync(int tenantId, InvoiceCreateDto dto)
        {
            // Invoice numbers, totals, currency, dates and status are authoritative
            // server-side values derived from the reservation. Client-supplied
            // financial fields are intentionally ignored.
            return await GenerateInvoiceForReservationAsync(tenantId, dto.ReservationID);
        }

        public async Task<ServiceResult<PaymentDto>> RecordPaymentAsync(int tenantId, string userId, PaymentCreateDto dto)
        {
            if (dto.Amount <= 0)
            {
                return ServiceResult<PaymentDto>.Failure(
                    "قيمة الدفعة يجب أن تكون أكبر من صفر",
                    "INVALID_PAYMENT_AMOUNT");
            }

            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Payments)
                .Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.InvoiceID == dto.InvoiceID && i.TenantID == tenantId);

            if (invoice == null)
            {
                return ServiceResult<PaymentDto>.Failure("الفاتورة غير موجودة", "INVOICE_NOT_FOUND");
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                return ServiceResult<PaymentDto>.Failure("الفاتورة مدفوعة بالكامل بالفعل", "INVOICE_ALREADY_PAID");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return ServiceResult<PaymentDto>.Failure("لا يمكن سداد دفعة لفاتورة ملغاة", "INVOICE_CANCELLED");
            }

            if (!string.Equals(
                    invoice.CurrencyCode,
                    dto.CurrencyCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<PaymentDto>.Failure(
                    "عملة الدفعة يجب أن تطابق عملة الفاتورة",
                    "PAYMENT_CURRENCY_MISMATCH");
            }

            decimal totalPaidBefore = invoice.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            decimal totalCredits = invoice.CreditNotes.Sum(note => note.Amount);
            decimal totalRefunded = invoice.CreditNotes
                .Where(note => note.Type == CreditNoteType.Refund)
                .Sum(note => note.Amount);
            var balance = InvoiceBalanceCalculator.Calculate(
                invoice.TotalAmount,
                totalPaidBefore,
                totalCredits,
                totalRefunded);

            decimal totalPaidAfter = balance.NetPaid + dto.Amount;

            if (totalPaidAfter > balance.AdjustedTotal)
            {
                return ServiceResult<PaymentDto>.Failure("قيمة الدفعة تتجاوز القيمة المتبقية للفاتورة", "PAYMENT_AMOUNT_EXCEEDS_REMAINING");
            }

            if (!Enum.TryParse<PaymentMethod>(dto.Method, true, out var method))
            {
                return ServiceResult<PaymentDto>.Failure("طريقة الدفع غير صالحة", "INVALID_PAYMENT_METHOD");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = new Payment
                {
                    TenantID = tenantId,
                    InvoiceID = dto.InvoiceID,
                    PaymentDate = DateTime.UtcNow,
                    Amount = dto.Amount,
                    CurrencyCode = dto.CurrencyCode,
                    Method = method,
                    ReferenceNo = dto.ReferenceNo,
                    Status = PaymentStatus.Completed,
                    Notes = dto.Notes,
                    ReceivedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Payments.AddAsync(payment);

                if (totalPaidAfter >= balance.AdjustedTotal)
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
                    .FirstOrDefaultAsync(p => p.PaymentID == payment.PaymentID);

                var resultDto = _mapper.Map<PaymentDto>(fullPayment ?? payment);
                return ServiceResult<PaymentDto>.Success(resultDto);
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<PaymentDto>.Failure(
                    "تغير رصيد الفاتورة أثناء تسجيل الدفعة. أعد تحميل الفاتورة وحاول مرة أخرى.",
                    "PAYMENT_CONFLICT");
            }
            catch (DbUpdateException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<PaymentDto>.Failure(
                    "تعذر تسجيل الدفعة بسبب تعارض في البيانات.",
                    "PAYMENT_CONFLICT");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<PaymentDto>.Failure(
                    "تعذر تسجيل الدفعة. استخدم رقم التتبع عند التواصل مع الدعم.",
                    "PAYMENT_RECORDING_FAILED");
            }
        }

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
                .ToListAsync();

            var dtos = _mapper.Map<IList<PaymentDto>>(payments);
            return ServiceResult<IList<PaymentDto>>.Success(dtos);
        }

        public async Task<ServiceResult<CreditNoteDto>> CreateCreditNoteAsync(
            int tenantId,
            string userId,
            int invoiceId,
            CreditNoteCreateDto dto)
        {
            if (!Enum.TryParse<CreditNoteType>(dto.Type, true, out var noteType))
            {
                return ServiceResult<CreditNoteDto>.Failure(
                    "نوع المستند يجب أن يكون Credit أو Refund",
                    "INVALID_CREDIT_NOTE_TYPE");
            }

            if (dto.Amount <= 0)
            {
                return ServiceResult<CreditNoteDto>.Failure(
                    "قيمة الإشعار الدائن يجب أن تكون أكبر من صفر",
                    "INVALID_CREDIT_NOTE_AMOUNT");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);
            try
            {
                var invoice = await _dbContext.Invoices
                    .Include(candidate => candidate.Payments)
                    .Include(candidate => candidate.CreditNotes)
                    .SingleOrDefaultAsync(candidate =>
                        candidate.InvoiceID == invoiceId &&
                        candidate.TenantID == tenantId);

                if (invoice == null)
                {
                    return ServiceResult<CreditNoteDto>.Failure(
                        "الفاتورة غير موجودة",
                        "INVOICE_NOT_FOUND");
                }

                if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
                {
                    return ServiceResult<CreditNoteDto>.Failure(
                        "لا يمكن إصدار إشعار دائن لفاتورة مسودة أو ملغاة",
                        "INVOICE_NOT_ISSUED");
                }

                var previousAdjustments = invoice.CreditNotes.Sum(note => note.Amount);
                var grossPaid = invoice.Payments
                    .Where(payment => payment.Status == PaymentStatus.Completed)
                    .Sum(payment => payment.Amount);
                var previousRefunds = invoice.CreditNotes
                    .Where(note => note.Type == CreditNoteType.Refund)
                    .Sum(note => note.Amount);
                if (previousAdjustments + dto.Amount > invoice.TotalAmount)
                {
                    return ServiceResult<CreditNoteDto>.Failure(
                        "قيمة الإشعارات الدائنة تتجاوز إجمالي الفاتورة",
                        "CREDIT_NOTE_EXCEEDS_INVOICE");
                }

                if (noteType == CreditNoteType.Refund)
                {
                    if (previousRefunds + dto.Amount > grossPaid)
                    {
                        return ServiceResult<CreditNoteDto>.Failure(
                            "قيمة الاسترداد تتجاوز المبلغ المدفوع المتاح",
                            "REFUND_EXCEEDS_PAID_AMOUNT");
                    }
                }

                var creditNote = new CreditNote
                {
                    TenantID = tenantId,
                    InvoiceID = invoiceId,
                    CreditNoteNumber = $"CN-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..20],
                    Type = noteType,
                    Amount = dto.Amount,
                    CurrencyCode = invoice.CurrencyCode,
                    Reason = dto.Reason.Trim(),
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.CreditNotes.Add(creditNote);

                var totalRefundedAfter = previousRefunds +
                    (noteType == CreditNoteType.Refund ? dto.Amount : 0);
                var balanceAfter = InvoiceBalanceCalculator.Calculate(
                    invoice.TotalAmount,
                    grossPaid,
                    previousAdjustments + dto.Amount,
                    totalRefundedAfter);
                invoice.Status = balanceAfter.NetPaid >= balanceAfter.AdjustedTotal
                    ? InvoiceStatus.Paid
                    : balanceAfter.NetPaid > 0
                        ? InvoiceStatus.PartiallyPaid
                        : InvoiceStatus.Issued;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<CreditNoteDto>.Success(
                    _mapper.Map<CreditNoteDto>(creditNote));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return ServiceResult<CreditNoteDto>.Failure(
                    "تعذر إصدار الإشعار الدائن بسبب تعارض في البيانات",
                    "CREDIT_NOTE_CONFLICT");
            }
        }

        public async Task<ServiceResult<IList<CreditNoteDto>>> GetCreditNotesByInvoiceAsync(
            int tenantId,
            int invoiceId)
        {
            var invoiceExists = await _dbContext.Invoices.AnyAsync(invoice =>
                invoice.InvoiceID == invoiceId && invoice.TenantID == tenantId);
            if (!invoiceExists)
            {
                return ServiceResult<IList<CreditNoteDto>>.Failure(
                    "الفاتورة غير موجودة",
                    "INVOICE_NOT_FOUND");
            }

            var notes = await _dbContext.CreditNotes
                .Where(note => note.InvoiceID == invoiceId && note.TenantID == tenantId)
                .OrderByDescending(note => note.CreatedAt)
                .ToListAsync();

            return ServiceResult<IList<CreditNoteDto>>.Success(
                _mapper.Map<IList<CreditNoteDto>>(notes));
        }
    }
}
