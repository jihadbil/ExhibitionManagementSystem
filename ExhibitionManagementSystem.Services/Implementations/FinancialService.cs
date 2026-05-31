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

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class FinancialService : IFinancialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FinancialService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<PagedResultDto<InvoiceDto>>> GetInvoicesByTenantAsync(int tenantId, int page, int pageSize)
        {
            var query = _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
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
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<InvoiceDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (reservation.Status != ReservationStatus.Confirmed)
            {
                return ServiceResult<InvoiceDto>.Failure("لا يمكن توليد فاتورة لحجز غير مؤكد", "RESERVATION_NOT_CONFIRMED");
            }

            var existing = await _unitOfWork.Invoices.GetByReservationAsync(reservationId);
            if (existing != null)
            {
                return ServiceResult<InvoiceDto>.Failure("تم إنشاء فاتورة مسبقاً لهذا الحجز", "INVOICE_ALREADY_EXISTS");
            }

            string invoiceNumber = await _unitOfWork.Invoices.GenerateNextInvoiceNumberAsync(tenantId);
            decimal taxRate = 15.0m; // Default VAT tax rate
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
                Status = InvoiceStatus.Issued,
                DueDate = DateTime.UtcNow.AddDays(30), // Default due date is 30 days from now
                Notes = $"Invoice for booth reservation #{reservationId}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            var fullInvoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Exhibitor)
                .Include(i => i.Currency)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);

            var dto = _mapper.Map<InvoiceDto>(fullInvoice ?? invoice);
            return ServiceResult<InvoiceDto>.Success(dto);
        }

        public async Task<ServiceResult<InvoiceDto>> CreateInvoiceAsync(int tenantId, InvoiceCreateDto dto)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .FirstOrDefaultAsync(r => r.ReservationID == dto.ReservationID);

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
                .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);

            var resultDto = _mapper.Map<InvoiceDto>(fullInvoice ?? invoice);
            return ServiceResult<InvoiceDto>.Success(resultDto);
        }

        public async Task<ServiceResult<PaymentDto>> RecordPaymentAsync(int tenantId, PaymentCreateDto dto)
        {
            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Payments)
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

            decimal totalPaidBefore = invoice.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            decimal totalPaidAfter = totalPaidBefore + dto.Amount;

            if (totalPaidAfter > invoice.TotalAmount)
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
                    InvoiceID = dto.InvoiceID,
                    PaymentDate = DateTime.UtcNow,
                    Amount = dto.Amount,
                    CurrencyCode = dto.CurrencyCode,
                    Method = method,
                    ReferenceNo = dto.ReferenceNo,
                    Status = PaymentStatus.Completed,
                    Notes = dto.Notes,
                    ReceivedByUserId = dto.ReceivedByUserId,
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
                    .FirstOrDefaultAsync(p => p.PaymentID == payment.PaymentID);

                var resultDto = _mapper.Map<PaymentDto>(fullPayment ?? payment);
                return ServiceResult<PaymentDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<PaymentDto>.Failure($"فشل تسجيل الدفعة: {ex.Message}", "PAYMENT_RECORDING_FAILED");
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
    }
}
