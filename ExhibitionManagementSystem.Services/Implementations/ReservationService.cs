using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class ReservationService : IReservationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPricingService _pricingService;
        private readonly IFinancialService _financialService;

        public ReservationService(IUnitOfWork unitOfWork, IMapper mapper, IPricingService pricingService, IFinancialService financialService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _pricingService = pricingService;
            _financialService = financialService;
        }

        public async Task<ServiceResult<PagedResultDto<BoothReservationSummaryDto>>> GetByExhibitionAsync(int tenantId, int exhibitionId, int page, int pageSize)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<PagedResultDto<BoothReservationSummaryDto>>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var query = _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.Booth)
                .Include(r => r.Exhibition)
                .Where(r => r.ExhibitionID == exhibitionId);

            var totalCount = await query.CountAsync(cancellationToken: _unitOfWork.RequestCancellation);
            var items = await query
                .OrderByDescending(r => r.ReservationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var dtos = _mapper.Map<IList<BoothReservationSummaryDto>>(items);
            var result = new PagedResultDto<BoothReservationSummaryDto>
            {
                Items = dtos.ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<BoothReservationSummaryDto>>.Success(result);
        }

        public async Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetByExhibitorAsync(int tenantId, int exhibitorId)
        {
            var exhibitor = await _unitOfWork.Exhibitors.GetByIdAsync(exhibitorId);
            if (exhibitor == null || exhibitor.TenantID != tenantId)
            {
                return ServiceResult<IList<BoothReservationSummaryDto>>.Failure("العارض غير موجود", "EXHIBITOR_NOT_FOUND");
            }

            var items = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.Booth)
                .Include(r => r.Exhibition)
                .Where(r => r.ExhibitorID == exhibitorId)
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var dtos = _mapper.Map<IList<BoothReservationSummaryDto>>(items);
            return ServiceResult<IList<BoothReservationSummaryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<BoothReservationDto>> GetByIdAsync(int tenantId, int reservationId)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.Booth)
                .Include(r => r.Exhibition)
                .Include(r => r.Currency)
                .Include(r => r.ReservationServices)
                    .ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<BoothReservationDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            var dto = _mapper.Map<BoothReservationDto>(reservation);
            return ServiceResult<BoothReservationDto>.Success(dto);
        }

        public async Task<ServiceResult<BoothReservationDto>> CreateAsync(int tenantId, string userId, BoothReservationCreateDto dto)
        {
            var exhibitor = await _unitOfWork.Exhibitors.GetByIdAsync(dto.ExhibitorID);
            if (exhibitor == null || exhibitor.TenantID != tenantId)
            {
                return ServiceResult<BoothReservationDto>.Failure("العارض غير موجود", "EXHIBITOR_NOT_FOUND");
            }

            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<BoothReservationDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (exhibition.Status != ExhibitionStatus.Open)
            {
                return ServiceResult<BoothReservationDto>.Failure("المعرض مغلق للحجوزات حالياً", "EXHIBITION_CLOSED");
            }

            if (dto.BoothID.HasValue)
            {
                var booth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(dto.BoothID.Value, b => b.Hall, b => b.Hall.Venue);
                if (booth == null || booth.Hall.Venue.TenantID != tenantId)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك غير موجود", "BOOTH_NOT_FOUND");
                }
                if (booth.Status != BoothStatus.Available)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك غير متاح للحجز", "BOOTH_NOT_AVAILABLE");
                }

                var isReserved = await _unitOfWork.BoothReservations.IsBoothReservedAsync(dto.BoothID.Value, dto.ExhibitionID);
                if (isReserved)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك محجوز بالفعل في هذا المعرض", "BOOTH_ALREADY_RESERVED");
                }
            }

            if (dto.MergeID.HasValue)
            {
                var merge = await _unitOfWork.BoothMerges.GetByIdAsync(dto.MergeID.Value);
                if (merge == null || merge.ExhibitionID != dto.ExhibitionID)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك المدمج غير موجود", "MERGE_NOT_FOUND");
                }

                var isReserved = await _unitOfWork.BoothReservations.IsMergeReservedAsync(dto.MergeID.Value, dto.ExhibitionID);
                if (isReserved)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك المدمج محجوز بالفعل في هذا المعرض", "MERGE_ALREADY_RESERVED");
                }
            }

            if (!Enum.TryParse<BoothType>(dto.BoothTypeSelected, true, out var boothType) || !Enum.IsDefined(boothType))
            {
                return ServiceResult<BoothReservationDto>.Failure("نوع الكشك غير صالح", "INVALID_BOOTH_TYPE");
            }

            if (!Enum.TryParse<ExhibitorCategory>(dto.ExhibitorCategory, true, out var exhibitorCategory) || !Enum.IsDefined(exhibitorCategory))
            {
                return ServiceResult<BoothReservationDto>.Failure("فئة العارض غير صالحة", "INVALID_EXHIBITOR_CATEGORY");
            }

            var priceResult = await _pricingService.CalculateBoothPriceAsync(tenantId, dto.ExhibitionID, boothType, exhibitorCategory, dto.RequestedAreaSqM, dto.BoothID, dto.CurrencyCode);
            if (!priceResult.IsSuccess)
            {
                return ServiceResult<BoothReservationDto>.Failure(priceResult.ErrorMessage ?? "فشل حساب سعر الكشك", priceResult.ErrorCode ?? "PRICING_ERROR");
            }

            decimal boothAmount = priceResult.Data;

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<BoothReservationDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            decimal exchangeRateUsed = 1.0m;
            if (!string.Equals(dto.CurrencyCode, tenant.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                var converted = await _unitOfWork.ExchangeRates.ConvertAsync(dto.CurrencyCode, tenant.BaseCurrency, 1.0m);
                if (converted == null)
                {
                    return ServiceResult<BoothReservationDto>.Failure($"لا يمكن حساب سعر الصرف من {dto.CurrencyCode} إلى {tenant.BaseCurrency}", "EXCHANGE_RATE_NOT_FOUND");
                }
                exchangeRateUsed = converted.Value;
            }

            decimal amountInBaseCurrency = boothAmount * exchangeRateUsed;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reservation = new BoothReservation
                {
                    ExhibitorID = dto.ExhibitorID,
                    BoothID = dto.BoothID,
                    ExhibitionID = dto.ExhibitionID,
                    MergeID = dto.MergeID,
                    BoothTypeSelected = boothType,
                    RequestedAreaSqM = dto.RequestedAreaSqM,
                    AllocatedAreaSqM = dto.BoothID.HasValue
                        ? (await _unitOfWork.Booths.GetByIdAsync(dto.BoothID.Value))?.OriginalAreaSqM ?? dto.RequestedAreaSqM
                        : dto.MergeID.HasValue
                            ? (await _unitOfWork.BoothMerges.GetByIdAsync(dto.MergeID.Value))?.TotalAreaSqM ?? dto.RequestedAreaSqM
                            : dto.RequestedAreaSqM,
                    ExhibitorCategory = exhibitorCategory,
                    BoothAmount = boothAmount,
                    ServicesAmount = 0,
                    TotalAmount = boothAmount,
                    CurrencyCode = dto.CurrencyCode,
                    ExchangeRateUsed = exchangeRateUsed,
                    AmountInBaseCurrency = amountInBaseCurrency,
                    Status = ReservationStatus.Pending,
                    ReservationDate = DateTime.UtcNow,
                    LogisticNotes = dto.LogisticNotes,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                if (dto.BoothID.HasValue)
                {
                    var booth = await _unitOfWork.Booths.GetByIdAsync(dto.BoothID.Value);
                    if (booth != null)
                    {
                        booth.Status = BoothStatus.Reserved;
                        _unitOfWork.Booths.Update(booth);
                    }
                }

                await _unitOfWork.BoothReservations.AddAsync(reservation);
                await _unitOfWork.SaveChangesAsync();

                // توليد الفاتورة المبدئية تلقائياً مع بنودها التفصيلية
                var invoiceResult = await _financialService.GenerateInvoiceForReservationAsync(tenantId, reservation.ReservationID);
                if (!invoiceResult.IsSuccess)
                {
                    throw new Exception(invoiceResult.ErrorMessage ?? "فشل توليد الفاتورة التفصيلية للحجز");
                }

                await _unitOfWork.CommitTransactionAsync();

                var fullReservation = await _unitOfWork.BoothReservations.AsQueryable()
                    .Include(r => r.Exhibitor)
                    .Include(r => r.Booth)
                    .Include(r => r.Exhibition)
                    .Include(r => r.Currency)
                    .FirstOrDefaultAsync(r => r.ReservationID == reservation.ReservationID, cancellationToken: _unitOfWork.RequestCancellation);

                var resultDto = _mapper.Map<BoothReservationDto>(fullReservation ?? reservation);
                return ServiceResult<BoothReservationDto>.Success(resultDto);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult<BoothReservationDto>> UpdateAsync(int tenantId, int id, BoothReservationUpdateDto dto)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.Booth)
                .Include(r => r.ReservationServices)
                .FirstOrDefaultAsync(r => r.ReservationID == id, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<BoothReservationDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (!Enum.TryParse<ReservationStatus>(dto.Status, true, out var newStatus) || !Enum.IsDefined(newStatus))
            {
                return ServiceResult<BoothReservationDto>.Failure("حالة الحجز غير صالحة", "INVALID_RESERVATION_STATUS");
            }

            // Reallocation/repricing requires a new booking; a general update
            // cannot silently move the financial document to another resource.
            if (dto.BoothID != reservation.BoothID || dto.MergeID != reservation.MergeID)
                return ServiceResult<BoothReservationDto>.Failure("لتغيير الجناح، ألغِ الحجز غير المسدد وأنشئ حجزًا جديدًا", "RESERVATION_RESOURCE_IMMUTABLE");
            if ((reservation.Status == ReservationStatus.Cancelled && newStatus != ReservationStatus.Cancelled)
                || (reservation.Status == ReservationStatus.Confirmed && newStatus == ReservationStatus.Pending))
                return ServiceResult<BoothReservationDto>.Failure("انتقال حالة الحجز غير مسموح", "INVALID_RESERVATION_TRANSITION");
            if (newStatus == ReservationStatus.Cancelled && await HasCompletedPaymentsAsync(id))
                return ServiceResult<BoothReservationDto>.Failure("تجب تسوية الدفعات قبل إلغاء الحجز", "RESERVATION_HAS_PAYMENTS");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var oldStatus = reservation.Status;
                var oldBoothId = reservation.BoothID;

                _mapper.Map(dto, reservation);
                reservation.UpdatedAt = DateTime.UtcNow;

                // Handle Booth Status changes based on Reservation Status
                if (oldStatus != newStatus)
                {
                    if (newStatus == ReservationStatus.Cancelled)
                    {
                        if (reservation.BoothID.HasValue)
                        {
                            var booth = await _unitOfWork.Booths.GetByIdAsync(reservation.BoothID.Value);
                            if (booth != null)
                            {
                                booth.Status = BoothStatus.Available;
                                _unitOfWork.Booths.Update(booth);
                            }
                        }
                    }
                    else if (newStatus == ReservationStatus.Confirmed)
                    {
                        if (reservation.BoothID.HasValue)
                        {
                            var booth = await _unitOfWork.Booths.GetByIdAsync(reservation.BoothID.Value);
                            if (booth != null)
                            {
                                booth.Status = BoothStatus.Reserved;
                                _unitOfWork.Booths.Update(booth);
                            }
                        }
                    }

                    // تحديث حالة الفاتورة المقابلة بناءً على حالة الحجز الجديدة
                    var invoice = await _unitOfWork.Invoices.AsQueryable()
                        .FirstOrDefaultAsync(i => i.ReservationID == id, cancellationToken: _unitOfWork.RequestCancellation);
                    if (invoice != null)
                    {
                        if (newStatus == ReservationStatus.Cancelled)
                        {
                            invoice.Status = InvoiceStatus.Cancelled;
                            _unitOfWork.Invoices.Update(invoice);
                        }
                        else if (newStatus == ReservationStatus.Confirmed && invoice.Status == InvoiceStatus.Draft)
                        {
                            invoice.Status = InvoiceStatus.Issued;
                            _unitOfWork.Invoices.Update(invoice);
                        }
                    }
                }

                // If BoothID was updated in DTO
                if (oldBoothId != dto.BoothID)
                {
                    // Free the old booth
                    if (oldBoothId.HasValue)
                    {
                        var oldBooth = await _unitOfWork.Booths.GetByIdAsync(oldBoothId.Value);
                        if (oldBooth != null)
                        {
                            oldBooth.Status = BoothStatus.Available;
                            _unitOfWork.Booths.Update(oldBooth);
                        }
                    }
                    // Reserve the new booth
                    if (dto.BoothID.HasValue)
                    {
                        var newBooth = await _unitOfWork.Booths.GetByIdAsync(dto.BoothID.Value);
                        if (newBooth != null)
                        {
                            newBooth.Status = newStatus == ReservationStatus.Cancelled ? BoothStatus.Available : BoothStatus.Reserved;
                            _unitOfWork.Booths.Update(newBooth);
                        }
                    }
                }

                _unitOfWork.BoothReservations.Update(reservation);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return await GetByIdAsync(tenantId, id);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult> CancelAsync(int tenantId, int id)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .FirstOrDefaultAsync(r => r.ReservationID == id, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return ServiceResult.Success();
            }

            if (await HasCompletedPaymentsAsync(id))
                return ServiceResult.Failure("تجب تسوية الدفعات قبل إلغاء الحجز", "RESERVATION_HAS_PAYMENTS");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.BoothReservations.Update(reservation);

                if (reservation.BoothID.HasValue)
                {
                    var booth = await _unitOfWork.Booths.GetByIdAsync(reservation.BoothID.Value);
                    if (booth != null)
                    {
                        booth.Status = BoothStatus.Available;
                        _unitOfWork.Booths.Update(booth);
                    }
                }

                // إلغاء الفاتورة المرافقة للحجز تلقائياً
                var invoice = await _unitOfWork.Invoices.AsQueryable()
                    .FirstOrDefaultAsync(i => i.ReservationID == id, cancellationToken: _unitOfWork.RequestCancellation);
                if (invoice != null)
                {
                    invoice.Status = InvoiceStatus.Cancelled;
                    _unitOfWork.Invoices.Update(invoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return ServiceResult.Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult<BoothReservationDto>> ApproveAsync(int tenantId, int id)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .FirstOrDefaultAsync(r => r.ReservationID == id, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<BoothReservationDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (reservation.Status == ReservationStatus.Confirmed)
            {
                return await GetByIdAsync(tenantId, id);
            }

            if (reservation.Status != ReservationStatus.Pending)
                return ServiceResult<BoothReservationDto>.Failure("لا يمكن اعتماد حجز ملغى", "INVALID_RESERVATION_TRANSITION");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.Status = ReservationStatus.Confirmed;
                reservation.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.BoothReservations.Update(reservation);

                if (reservation.BoothID.HasValue)
                {
                    var booth = await _unitOfWork.Booths.GetByIdAsync(reservation.BoothID.Value);
                    if (booth != null)
                    {
                        booth.Status = BoothStatus.Reserved;
                        _unitOfWork.Booths.Update(booth);
                    }
                }

                // تحويل الفاتورة المبدئية إلى فاتورة صادرة مستحقة
                var invoice = await _unitOfWork.Invoices.AsQueryable()
                    .FirstOrDefaultAsync(i => i.ReservationID == id, cancellationToken: _unitOfWork.RequestCancellation);
                if (invoice != null && invoice.Status == InvoiceStatus.Draft)
                {
                    invoice.Status = InvoiceStatus.Issued;
                    _unitOfWork.Invoices.Update(invoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return await GetByIdAsync(tenantId, id);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult<ReservationServiceDto>> AddServiceToReservationAsync(int tenantId, int reservationId, ReservationServiceCreateDto dto)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.ReservationServices)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<ReservationServiceDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (!await CanEditInvoiceAsync(reservationId))
                return ServiceResult<ReservationServiceDto>.Failure("الفاتورة المسددة أو الملغاة غير قابلة للتعديل", "INVOICE_LOCKED");

            var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceID);
            if (service == null || service.TenantID != tenantId)
            {
                return ServiceResult<ReservationServiceDto>.Failure("الخدمة غير موجودة", "SERVICE_NOT_FOUND");
            }

            if (!string.Equals(dto.CurrencyCode, reservation.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                return ServiceResult<ReservationServiceDto>.Failure("عملة الخدمة يجب أن تطابق الحجز", "SERVICE_CURRENCY_MISMATCH");
            if (reservation.Status == ReservationStatus.Cancelled)
                return ServiceResult<ReservationServiceDto>.Failure("لا يمكن تعديل حجز ملغى", "RESERVATION_CANCELLED");
            var servicePriceResult = await _pricingService.CalculateServicePriceAsync(tenantId, dto.ServiceID, reservation.ExhibitionID, dto.Quantity, reservation.CurrencyCode);
            if (!servicePriceResult.IsSuccess)
            {
                return ServiceResult<ReservationServiceDto>.Failure(servicePriceResult.ErrorMessage ?? "فشل حساب سعر الخدمة", servicePriceResult.ErrorCode ?? "PRICING_ERROR");
            }

            decimal totalPrice = servicePriceResult.Data;
            decimal unitPrice = dto.Quantity != 0 ? totalPrice / dto.Quantity : 0;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var resService = new Models.ReservationService
                {
                    ReservationID = reservationId,
                    ServiceID = dto.ServiceID,
                    Quantity = dto.Quantity,
                    UnitPrice = unitPrice,
                    CurrencyCode = dto.CurrencyCode,
                    TotalPrice = totalPrice
                };

                reservation.ReservationServices.Add(resService);
                reservation.ServicesAmount += totalPrice;
                reservation.TotalAmount += totalPrice;
                reservation.AmountInBaseCurrency = reservation.TotalAmount * reservation.ExchangeRateUsed;
                reservation.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.BoothReservations.Update(reservation);

                // إضافة البند المقابل للخدمة المضافة حديثاً في الفاتورة وإعادة احتساب القيم
                var invoice = await _unitOfWork.Invoices.AsQueryable()
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.ReservationID == reservationId, cancellationToken: _unitOfWork.RequestCancellation);
                if (invoice != null)
                {
                    var invoiceItem = new InvoiceItem
                    {
                        InvoiceID = invoice.InvoiceID,
                        ItemName = service.ServiceName,
                        Quantity = dto.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice
                    };
                    invoice.InvoiceItems.Add(invoiceItem);

                    invoice.SubTotal = invoice.InvoiceItems.Sum(item => item.TotalPrice);
                    invoice.TaxAmount = decimal.Round(invoice.SubTotal * invoice.TaxRate / 100, 2, MidpointRounding.AwayFromZero);
                    invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
                    _unitOfWork.Invoices.Update(invoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var mappedDto = _mapper.Map<ReservationServiceDto>(resService);
                mappedDto.ServiceName = service.ServiceName; // Populate navigation field manually if Mapper didn't
                return ServiceResult<ReservationServiceDto>.Success(mappedDto);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult> RemoveServiceFromReservationAsync(int tenantId, int reservationId, int rsId)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.ReservationServices)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId, cancellationToken: _unitOfWork.RequestCancellation);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (reservation.Status == ReservationStatus.Cancelled || !await CanEditInvoiceAsync(reservationId))
                return ServiceResult.Failure("الحجز أو الفاتورة غير قابل للتعديل", "INVOICE_LOCKED");

            var resService = reservation.ReservationServices.FirstOrDefault(rs => rs.ReservationServiceID == rsId);
            if (resService == null)
            {
                return ServiceResult.Failure("الخدمة المحددة غير موجودة بهذا الحجز", "RESERVATION_SERVICE_NOT_FOUND");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.ServicesAmount -= resService.TotalPrice;
                reservation.TotalAmount -= resService.TotalPrice;
                reservation.AmountInBaseCurrency = reservation.TotalAmount * reservation.ExchangeRateUsed;
                reservation.UpdatedAt = DateTime.UtcNow;

                reservation.ReservationServices.Remove(resService);
                _unitOfWork.ReservationServices.Remove(resService);
                _unitOfWork.BoothReservations.Update(reservation);

                // إزالة بند الفاتورة المقابل للخدمة المحذوفة وإعادة احتساب قيم الفاتورة
                var service = await _unitOfWork.Services.GetByIdAsync(resService.ServiceID);
                var serviceName = service?.ServiceName;
                
                var invoice = await _unitOfWork.Invoices.AsQueryable()
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.ReservationID == reservationId, cancellationToken: _unitOfWork.RequestCancellation);
                if (invoice != null && serviceName != null)
                {
                    var invoiceItem = invoice.InvoiceItems.FirstOrDefault(item => 
                        item.ItemName == serviceName && 
                        item.Quantity == resService.Quantity && 
                        item.UnitPrice == resService.UnitPrice);

                    if (invoiceItem != null)
                    {
                        invoice.InvoiceItems.Remove(invoiceItem);
                        _unitOfWork.InvoiceItems.Remove(invoiceItem);

                        invoice.SubTotal = invoice.InvoiceItems.Sum(item => item.TotalPrice);
                        invoice.TaxAmount = decimal.Round(invoice.SubTotal * invoice.TaxRate / 100, 2, MidpointRounding.AwayFromZero);
                        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
                        _unitOfWork.Invoices.Update(invoice);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult.Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetUnpaidAsync(int tenantId, int exhibitionId)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<IList<BoothReservationSummaryDto>>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var unpaidList = await _unitOfWork.BoothReservations.GetUnpaidReservationsAsync(exhibitionId);
            var dtos = _mapper.Map<IList<BoothReservationSummaryDto>>(unpaidList);
            return ServiceResult<IList<BoothReservationSummaryDto>>.Success(dtos);
        }

        private Task<bool> HasCompletedPaymentsAsync(int reservationId) =>
            _unitOfWork.Payments.AsQueryable().AnyAsync(p => p.Invoice.ReservationID == reservationId
                && p.Status == PaymentStatus.Completed, cancellationToken: _unitOfWork.RequestCancellation);

        private async Task<bool> CanEditInvoiceAsync(int reservationId) =>
            !await _unitOfWork.Invoices.AsQueryable().AnyAsync(i => i.ReservationID == reservationId
                && (i.Status == InvoiceStatus.Paid || i.Status == InvoiceStatus.PartiallyPaid
                    || i.Status == InvoiceStatus.Cancelled || i.Payments.Any(p => p.Status == PaymentStatus.Completed)), cancellationToken: _unitOfWork.RequestCancellation);
    }
}
