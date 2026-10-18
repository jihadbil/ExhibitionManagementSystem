using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.ReservationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
                .ToListAsync();

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
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<BoothReservationDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            var dto = _mapper.Map<BoothReservationDto>(reservation);
            return ServiceResult<BoothReservationDto>.Success(dto);
        }

        public async Task<ServiceResult<BoothReservationDto>> CreateAsync(int tenantId, string userId, BoothReservationCreateDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try { return await CreateCoreAsync(tenantId, userId, dto); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 1205 or 1222)
            { return ServiceResult<BoothReservationDto>.Failure("تعارض الحجز مع عملية أخرى؛ أعد تحميل البيانات", "RESERVATION_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<BoothReservationDto>> CreateCoreAsync(int tenantId, string userId, BoothReservationCreateDto dto)
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

            if (exhibition.Status == ExhibitionStatus.Closed || exhibition.Status == ExhibitionStatus.Cancelled)
            {
                return ServiceResult<BoothReservationDto>.Failure("المعرض مغلق أو ملغى ولا يقبل حجوزات حالياً", "EXHIBITION_CLOSED");
            }

            Booth? selectedBooth = null;
            BoothMerge? selectedMerge = null;
            if (dto.BoothID.HasValue)
            {
                selectedBooth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(dto.BoothID.Value, b => b.Hall, b => b.Hall.Venue);
                if (selectedBooth == null || selectedBooth.Hall.Venue.TenantID != tenantId)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك غير موجود", "BOOTH_NOT_FOUND");
                }
                if (selectedBooth.Hall.VenueID != exhibition.VenueID)
                {
                    return ServiceResult<BoothReservationDto>.Failure(
                        "الجناح المحدد لا يتبع موقع هذا المعرض",
                        "BOOTH_NOT_IN_EXHIBITION_VENUE");
                }
                if (selectedBooth.Status == BoothStatus.Maintenance || await _unitOfWork.BoothMergeItems.ExistsAsync(
                    i => i.BoothID == selectedBooth.BoothID && i.ExhibitionID == dto.ExhibitionID))
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
                selectedMerge = await _unitOfWork.BoothMerges.AsQueryable()
                    .Include(m => m.MergeItems).ThenInclude(i => i.Booth).ThenInclude(b => b.Hall)
                    .FirstOrDefaultAsync(m => m.MergeID == dto.MergeID.Value);
                if (selectedMerge == null || selectedMerge.ExhibitionID != dto.ExhibitionID)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك المدمج غير موجود", "MERGE_NOT_FOUND");
                }
                if (selectedMerge.MergeItems.Count < 2 || selectedMerge.MergeItems.Any(i =>
                    i.Booth == null || i.Booth.IsDeleted || i.Booth.Hall.VenueID != exhibition.VenueID ||
                    i.Booth.Status == BoothStatus.Maintenance))
                    return ServiceResult<BoothReservationDto>.Failure("الجناح المدمج غير صالح لموقع هذا المعرض", "INVALID_BOOTH_MERGE");

                var isReserved = await _unitOfWork.BoothReservations.IsMergeReservedAsync(dto.MergeID.Value, dto.ExhibitionID);
                if (isReserved)
                {
                    return ServiceResult<BoothReservationDto>.Failure("الكشك المدمج محجوز بالفعل في هذا المعرض", "MERGE_ALREADY_RESERVED");
                }
            }

            var memberTypes = selectedMerge?.MergeItems.Select(i => i.Booth.BoothType).Distinct().ToList();
            var boothType = selectedBooth?.BoothType ?? (memberTypes?.Count == 1 ? memberTypes[0] : BoothType.Custom);

            var exhibitorCategory = exhibitor.ExhibitorCategory;
            var pricingArea = selectedBooth?.CurrentAreaSqM
                ?? selectedMerge?.MergeItems.Sum(i => i.Booth.CurrentAreaSqM)
                ?? dto.RequestedAreaSqM;

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<BoothReservationDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            dto.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode)
                ? exhibitor.PreferredCurrencyCode
                : dto.CurrencyCode.Trim().ToUpperInvariant();
            var billingCurrency = await _unitOfWork.Currencies.FirstOrDefaultAsync(
                currency => currency.CurrencyCode == dto.CurrencyCode && currency.IsActive);
            if (billingCurrency == null)
            {
                return ServiceResult<BoothReservationDto>.Failure("عملة فوترة العارض غير موجودة أو غير نشطة", "INVALID_BILLING_CURRENCY");
            }

            var pricingSnapshots = new List<object>();
            ExhibitionManagementSystem.Models.DTOs.Pricing.BoothPriceCalculationDto price;
            if (selectedMerge != null)
            {
                decimal mergedAmount = 0;
                foreach (var member in selectedMerge.MergeItems.OrderBy(i => i.SequenceOrder))
                {
                    var result = await _pricingService.CalculateBoothPriceDetailsAsync(tenantId, dto.ExhibitionID,
                        member.Booth.BoothType, exhibitorCategory, member.Booth.CurrentAreaSqM, member.BoothID, exhibitor.Sector);
                    if (!result.IsSuccess)
                        return ServiceResult<BoothReservationDto>.Failure(result.ErrorMessage!, result.ErrorCode!);
                    var memberPrice = result.Data!;
                    var conversion = await _unitOfWork.ExchangeRates.ConvertWithDetailsAsync(memberPrice.CurrencyCode,
                        dto.CurrencyCode, memberPrice.Amount, DateTime.UtcNow, tenant.BaseCurrency);
                    if (conversion == null)
                        return ServiceResult<BoothReservationDto>.Failure("سعر صرف أحد أجنحة الدمج غير متوفر", "EXCHANGE_RATE_NOT_FOUND");
                    mergedAmount += conversion.TargetAmount;
                    pricingSnapshots.Add(new { member.BoothID, member.Booth.BoothNumber, member.Booth.BoothType,
                        Area = member.Booth.CurrentAreaSqM, memberPrice.RuleID, SourceAmount = memberPrice.Amount,
                        SourceCurrency = memberPrice.CurrencyCode, BillingCurrency = dto.CurrencyCode,
                        conversion.ExchangeRate, conversion.RateDate, conversion.Source, conversion.TargetAmount });
                }
                // Each member retains its source/rate in the transactional audit snapshot.
                price = new() { Amount = mergedAmount, CurrencyCode = dto.CurrencyCode };
            }
            else
            {
                var result = await _pricingService.CalculateBoothPriceDetailsAsync(tenantId, dto.ExhibitionID,
                    boothType, exhibitorCategory, pricingArea, dto.BoothID, exhibitor.Sector);
                if (!result.IsSuccess)
                    return ServiceResult<BoothReservationDto>.Failure(result.ErrorMessage!, result.ErrorCode!);
                price = result.Data!;
            }
            var boothConversion = await _unitOfWork.ExchangeRates.ConvertWithDetailsAsync(
                price.CurrencyCode,
                dto.CurrencyCode,
                price.Amount,
                DateTime.UtcNow,
                tenant.BaseCurrency);
            if (boothConversion == null)
            {
                return ServiceResult<BoothReservationDto>.Failure(
                    $"لا يمكن حساب سعر الصرف من {price.CurrencyCode} إلى {dto.CurrencyCode}",
                    "EXCHANGE_RATE_NOT_FOUND");
            }
            decimal boothAmount = boothConversion.TargetAmount;

            var baseConversion = await _unitOfWork.ExchangeRates.ConvertWithDetailsAsync(
                dto.CurrencyCode,
                tenant.BaseCurrency,
                1m,
                DateTime.UtcNow,
                tenant.BaseCurrency);
            if (baseConversion == null)
            {
                return ServiceResult<BoothReservationDto>.Failure(
                    $"لا يمكن حساب سعر الصرف من {dto.CurrencyCode} إلى {tenant.BaseCurrency}",
                    "EXCHANGE_RATE_NOT_FOUND");
            }

            var serviceRequests = dto.Services?.ToList() ?? [];
            var mandatoryServices = await _unitOfWork.Services.GetMandatoryServicesAsync(tenantId);
            foreach (var mandatoryService in mandatoryServices.Where(service => service.IsActive))
            {
                if (serviceRequests.All(item => item.ServiceID != mandatoryService.ServiceID))
                {
                    serviceRequests.Add(new ReservationServiceCreateDto
                    {
                        ServiceID = mandatoryService.ServiceID,
                        Quantity = 1,
                        CurrencyCode = dto.CurrencyCode
                    });
                }
            }

            var requestedServices = new List<(
                Service Service,
                decimal Quantity,
                decimal UnitPrice,
                decimal TotalPrice,
                decimal SourceUnitPrice,
                decimal SourceTotalPrice,
                string SourceCurrencyCode,
                decimal ExchangeRate,
                DateTime RateDate,
                string RateSource,
                int? PriceRuleID)>();
            var duplicateServiceId = serviceRequests
                .GroupBy(item => item.ServiceID)
                .FirstOrDefault(group => group.Count() > 1)?.Key;
            if (duplicateServiceId.HasValue)
            {
                return ServiceResult<BoothReservationDto>.Failure(
                    "لا يمكن إضافة الخدمة نفسها أكثر من مرة إلى الحجز",
                    "DUPLICATE_RESERVATION_SERVICE");
            }

            foreach (var requestedService in serviceRequests)
            {
                if (requestedService.Quantity <= 0 ||
                    requestedService.Quantity != decimal.Truncate(requestedService.Quantity) ||
                    requestedService.Quantity > int.MaxValue)
                {
                    return ServiceResult<BoothReservationDto>.Failure(
                        "كمية الخدمة يجب أن تكون عدداً صحيحاً موجباً",
                        "INVALID_SERVICE_QUANTITY");
                }

                var service = await _unitOfWork.Services.GetByIdAsync(requestedService.ServiceID);
                if (service == null || service.TenantID != tenantId || !service.IsActive)
                {
                    return ServiceResult<BoothReservationDto>.Failure(
                        "إحدى الخدمات المحددة غير موجودة أو غير متاحة",
                        "SERVICE_NOT_AVAILABLE");
                }

                var servicePriceResult = await _pricingService.CalculateServicePriceDetailsAsync(
                    tenantId,
                    service.ServiceID,
                    dto.ExhibitionID,
                    (int)requestedService.Quantity,
                    exhibitorCategory);
                if (!servicePriceResult.IsSuccess)
                {
                    return ServiceResult<BoothReservationDto>.Failure(
                        servicePriceResult.ErrorMessage ?? $"فشل حساب سعر خدمة {service.ServiceName}",
                        servicePriceResult.ErrorCode ?? "SERVICE_PRICING_ERROR");
                }

                var servicePrice = servicePriceResult.Data!;
                var serviceConversion = await _unitOfWork.ExchangeRates.ConvertWithDetailsAsync(
                    servicePrice.CurrencyCode,
                    dto.CurrencyCode,
                    servicePrice.TotalAmount,
                    DateTime.UtcNow,
                    tenant.BaseCurrency);
                if (serviceConversion == null)
                {
                    return ServiceResult<BoothReservationDto>.Failure(
                        $"لا يمكن حساب سعر خدمة {service.ServiceName} من {servicePrice.CurrencyCode} إلى {dto.CurrencyCode}",
                        "EXCHANGE_RATE_NOT_FOUND");
                }

                var serviceTotal = serviceConversion.TargetAmount;
                requestedServices.Add((
                    service,
                    requestedService.Quantity,
                    servicePrice.UnitPrice * serviceConversion.ExchangeRate,
                    serviceTotal,
                    servicePrice.UnitPrice,
                    servicePrice.TotalAmount,
                    servicePrice.CurrencyCode,
                    serviceConversion.ExchangeRate,
                    serviceConversion.RateDate,
                    serviceConversion.Source,
                    servicePrice.PriceRuleID));
            }

            decimal servicesAmount = requestedServices.Sum(item => item.TotalPrice);
            decimal reservationTotal = boothAmount + servicesAmount;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reservation = new BoothReservation
                {
                    TenantID = tenantId,
                    ExhibitorID = dto.ExhibitorID,
                    BoothID = dto.BoothID,
                    ExhibitionID = dto.ExhibitionID,
                    MergeID = dto.MergeID,
                    BoothTypeSelected = boothType,
                    RequestedAreaSqM = pricingArea,
                    AllocatedAreaSqM = pricingArea,
                    ExhibitorCategory = exhibitorCategory,
                    BoothAmount = boothAmount,
                    ServicesAmount = servicesAmount,
                    TotalAmount = reservationTotal,
                    CurrencyCode = dto.CurrencyCode,
                    ExchangeRateUsed = baseConversion.ExchangeRate,
                    AmountInBaseCurrency = decimal.Round(reservationTotal * baseConversion.ExchangeRate, 2, MidpointRounding.AwayFromZero),
                    BaseCurrencyCode = tenant.BaseCurrency,
                    ExchangeRateDate = baseConversion.RateDate,
                    ExchangeRateSource = baseConversion.Source,
                    BoothSourceAmount = price.Amount,
                    BoothSourceCurrencyCode = price.CurrencyCode,
                    BoothExchangeRateUsed = boothConversion.ExchangeRate,
                    BoothExchangeRateDate = boothConversion.RateDate,
                    BoothExchangeRateSource = boothConversion.Source,
                    BoothPriceRuleID = price.RuleID > 0 ? price.RuleID : null,
                    Status = ReservationStatus.Pending,
                    ReservationDate = DateTime.UtcNow,
                    LogisticNotes = dto.LogisticNotes?.Trim() ?? string.Empty,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var requestedService in requestedServices)
                {
                    reservation.ReservationServices.Add(new Models.ReservationService
                    {
                        ServiceID = requestedService.Service.ServiceID,
                        Quantity = requestedService.Quantity,
                        UnitPrice = requestedService.UnitPrice,
                        CurrencyCode = dto.CurrencyCode,
                        TotalPrice = requestedService.TotalPrice,
                        SourceUnitPrice = requestedService.SourceUnitPrice,
                        SourceTotalPrice = requestedService.SourceTotalPrice,
                        SourceCurrencyCode = requestedService.SourceCurrencyCode,
                        ExchangeRateUsed = requestedService.ExchangeRate,
                        ExchangeRateDate = requestedService.RateDate,
                        ExchangeRateSource = requestedService.RateSource,
                        ServicePriceRuleID = requestedService.PriceRuleID
                    });
                }

                await _unitOfWork.BoothReservations.AddAsync(reservation);
                await _unitOfWork.SaveChangesAsync();
                if (pricingSnapshots.Count > 0)
                {
                    await _unitOfWork.AuditLogs.AddAsync(new AuditLog {
                        TenantID = tenantId, UserId = userId, TableName = "ReservationBoothPricing",
                        RecordID = reservation.ReservationID.ToString(), Action = "PriceSnapshot", ActionAt = DateTime.UtcNow,
                        NewValues = JsonSerializer.Serialize(pricingSnapshots)
                    });
                    await _unitOfWork.SaveChangesAsync();
                }

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
                    .Include(r => r.ReservationServices)
                        .ThenInclude(rs => rs.Service)
                    .FirstOrDefaultAsync(r => r.ReservationID == reservation.ReservationID);

                var resultDto = _mapper.Map<BoothReservationDto>(fullReservation ?? reservation);
                return ServiceResult<BoothReservationDto>.Success(resultDto);
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure(
                    "تغيرت حالة الحجز أثناء العملية. أعد تحميل البيانات وحاول مرة أخرى.",
                    "RESERVATION_CONFLICT");
            }
            catch (DbUpdateException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure(
                    "تعذر إتمام الحجز لأن الجناح حُجز بالتزامن مع طلبك.",
                    "RESERVATION_CONFLICT");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure("تعذر إنشاء الحجز.", "RESERVATION_CREATION_FAILED");
            }
        }

        public async Task<ServiceResult<BoothReservationDto>> UpdateAsync(int tenantId, int id, BoothReservationUpdateDto dto)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.Booth)
                .Include(r => r.ReservationServices)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<BoothReservationDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (!EnumInput.TryParse<ReservationStatus>(dto.Status, out var newStatus))
            {
                return ServiceResult<BoothReservationDto>.Failure("حالة الحجز غير صالحة", "INVALID_RESERVATION_STATUS");
            }

            if (!Enum.IsDefined(newStatus) ||
                (reservation.Status == ReservationStatus.Cancelled && newStatus != ReservationStatus.Cancelled) ||
                (reservation.Status == ReservationStatus.Confirmed && newStatus == ReservationStatus.Pending))
                return ServiceResult<BoothReservationDto>.Failure("انتقال حالة الحجز غير مسموح", "INVALID_RESERVATION_TRANSITION");

            var currentInvoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Payments).Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.ReservationID == id);
            if (newStatus == ReservationStatus.Cancelled && HasUnsettledPayments(currentInvoice))
                return ServiceResult<BoothReservationDto>.Failure("يجب تسوية المدفوعات والاسترداد قبل إلغاء الحجز", "RESERVATION_HAS_PAYMENTS");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var oldStatus = reservation.Status;

                _mapper.Map(dto, reservation);
                reservation.UpdatedAt = DateTime.UtcNow;

                // حالة الحجز تخص المعرض ولا تغير الحالة التشغيلية العامة للجناح.
                if (oldStatus != newStatus)
                {
                    // تحديث حالة الفاتورة المقابلة بناءً على حالة الحجز الجديدة
                    var invoice = await _unitOfWork.Invoices.AsQueryable()
                        .FirstOrDefaultAsync(i => i.ReservationID == id);
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

                _unitOfWork.BoothReservations.Update(reservation);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return await GetByIdAsync(tenantId, id);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure("تعذر تحديث الحجز.", "RESERVATION_UPDATE_FAILED");
            }
        }

        public async Task<ServiceResult> CancelAsync(int tenantId, int id)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return ServiceResult.Success();
            }

            var currentInvoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Payments).Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.ReservationID == id);
            if (HasUnsettledPayments(currentInvoice))
                return ServiceResult.Failure("يجب تسوية المدفوعات والاسترداد قبل إلغاء الحجز", "RESERVATION_HAS_PAYMENTS");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.BoothReservations.Update(reservation);

                // إلغاء الفاتورة المرافقة للحجز تلقائياً
                var invoice = await _unitOfWork.Invoices.AsQueryable()
                    .FirstOrDefaultAsync(i => i.ReservationID == id);
                if (invoice != null)
                {
                    invoice.Status = InvoiceStatus.Cancelled;
                    _unitOfWork.Invoices.Update(invoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult.Failure("تعذر إلغاء الحجز.", "CANCEL_FAILED");
            }
        }

        public async Task<ServiceResult<BoothReservationDto>> ApproveAsync(int tenantId, int id)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

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

                // تحويل الفاتورة المبدئية إلى فاتورة صادرة مستحقة
                var invoice = await _unitOfWork.Invoices.AsQueryable()
                    .FirstOrDefaultAsync(i => i.ReservationID == id);
                if (invoice != null && invoice.Status == InvoiceStatus.Draft)
                {
                    invoice.Status = InvoiceStatus.Issued;
                    _unitOfWork.Invoices.Update(invoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return await GetByIdAsync(tenantId, id);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure("تعذر تأكيد الحجز.", "APPROVE_FAILED");
            }
        }

        public async Task<ServiceResult<ReservationServiceDto>> AddServiceToReservationAsync(int tenantId, int reservationId, ReservationServiceCreateDto dto)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.ReservationServices)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult<ReservationServiceDto>.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return ServiceResult<ReservationServiceDto>.Failure(
                    "لا يمكن إضافة خدمات إلى حجز ملغى",
                    "RESERVATION_CANCELLED");
            }

            if (dto.Quantity <= 0 ||
                dto.Quantity != decimal.Truncate(dto.Quantity) ||
                dto.Quantity > int.MaxValue)
            {
                return ServiceResult<ReservationServiceDto>.Failure(
                    "كمية الخدمة يجب أن تكون عدداً صحيحاً موجباً",
                    "INVALID_SERVICE_QUANTITY");
            }

            if (reservation.ReservationServices.Any(item => item.ServiceID == dto.ServiceID))
            {
                return ServiceResult<ReservationServiceDto>.Failure(
                    "الخدمة مضافة مسبقاً إلى هذا الحجز",
                    "DUPLICATE_RESERVATION_SERVICE");
            }

            var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceID);
            if (service == null || service.TenantID != tenantId || !service.IsActive)
            {
                return ServiceResult<ReservationServiceDto>.Failure("الخدمة غير موجودة أو غير متاحة", "SERVICE_NOT_FOUND");
            }

            var servicePriceResult = await _pricingService.CalculateServicePriceDetailsAsync(
                tenantId,
                dto.ServiceID,
                reservation.ExhibitionID,
                (int)dto.Quantity,
                reservation.ExhibitorCategory);
            if (!servicePriceResult.IsSuccess)
            {
                return ServiceResult<ReservationServiceDto>.Failure(servicePriceResult.ErrorMessage ?? "فشل حساب سعر الخدمة", servicePriceResult.ErrorCode ?? "PRICING_ERROR");
            }

            var servicePrice = servicePriceResult.Data!;
            var conversion = await _unitOfWork.ExchangeRates.ConvertWithDetailsAsync(
                servicePrice.CurrencyCode,
                reservation.CurrencyCode,
                servicePrice.TotalAmount,
                DateTime.UtcNow,
                reservation.BaseCurrencyCode);
            if (conversion == null)
            {
                return ServiceResult<ReservationServiceDto>.Failure(
                    $"لا يمكن حساب سعر الخدمة من {servicePrice.CurrencyCode} إلى {reservation.CurrencyCode}",
                    "EXCHANGE_RATE_NOT_FOUND");
            }

            decimal totalPrice = conversion.TargetAmount;
            decimal unitPrice = servicePrice.UnitPrice * conversion.ExchangeRate;

            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.ReservationID == reservationId);
            if (invoice != null && invoice.Status != InvoiceStatus.Draft)
            {
                return ServiceResult<ReservationServiceDto>.Failure(
                    "لا يمكن تعديل الخدمات بعد إصدار الفاتورة؛ استخدم إشعاراً مالياً",
                    "INVOICE_IMMUTABLE");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var resService = new Models.ReservationService
                {
                    ReservationID = reservationId,
                    ServiceID = dto.ServiceID,
                    Quantity = dto.Quantity,
                    UnitPrice = unitPrice,
                    CurrencyCode = reservation.CurrencyCode,
                    TotalPrice = totalPrice,
                    SourceUnitPrice = servicePrice.UnitPrice,
                    SourceTotalPrice = servicePrice.TotalAmount,
                    SourceCurrencyCode = servicePrice.CurrencyCode,
                    ExchangeRateUsed = conversion.ExchangeRate,
                    ExchangeRateDate = conversion.RateDate,
                    ExchangeRateSource = conversion.Source,
                    ServicePriceRuleID = servicePrice.PriceRuleID
                };

                reservation.ReservationServices.Add(resService);
                reservation.ServicesAmount += totalPrice;
                reservation.TotalAmount += totalPrice;
                reservation.AmountInBaseCurrency = decimal.Round(
                    reservation.TotalAmount * reservation.ExchangeRateUsed,
                    2,
                    MidpointRounding.AwayFromZero);
                reservation.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.BoothReservations.Update(reservation);

                // إضافة البند المقابل للخدمة المضافة حديثاً في الفاتورة وإعادة احتساب القيم
                if (invoice != null)
                {
                    var invoiceItem = new InvoiceItem
                    {
                        ReservationService = resService,
                        InvoiceID = invoice.InvoiceID,
                        ItemName = service.ServiceName,
                        Quantity = dto.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice
                    };
                    invoice.InvoiceItems.Add(invoiceItem);

                    invoice.SubTotal = invoice.InvoiceItems.Sum(item => item.TotalPrice);
                    invoice.TaxAmount = InvoiceFinancialSummary.Round(invoice.SubTotal * (invoice.TaxRate / 100));
                    invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
                    invoice.TotalAmountInBaseCurrency = decimal.Round(
                        invoice.TotalAmount * invoice.ExchangeRateUsed,
                        2,
                        MidpointRounding.AwayFromZero);
                    _unitOfWork.Invoices.Update(invoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var mappedDto = _mapper.Map<ReservationServiceDto>(resService);
                mappedDto.ServiceName = service.ServiceName; // Populate navigation field manually if Mapper didn't
                return ServiceResult<ReservationServiceDto>.Success(mappedDto);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<ReservationServiceDto>.Failure("تعذر إضافة الخدمة للحجز.", "ADD_SERVICE_FAILED");
            }
        }

        public async Task<ServiceResult> RemoveServiceFromReservationAsync(int tenantId, int reservationId, int rsId)
        {
            var reservation = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Exhibitor)
                .Include(r => r.ReservationServices)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null || reservation.Exhibitor.TenantID != tenantId)
            {
                return ServiceResult.Failure("الحجز غير موجود", "RESERVATION_NOT_FOUND");
            }

            var resService = reservation.ReservationServices.FirstOrDefault(rs => rs.ReservationServiceID == rsId);
            if (resService == null)
            {
                return ServiceResult.Failure("الخدمة المحددة غير موجودة بهذا الحجز", "RESERVATION_SERVICE_NOT_FOUND");
            }

            var reservedService = await _unitOfWork.Services.GetByIdAsync(resService.ServiceID);
            if (reservedService?.IsMandatory == true)
                return ServiceResult.Failure("لا يمكن حذف خدمة إلزامية من الحجز", "MANDATORY_SERVICE_REQUIRED");

            var invoice = await _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.ReservationID == reservationId);
            if (invoice != null && invoice.Status != InvoiceStatus.Draft)
            {
                return ServiceResult.Failure(
                    "لا يمكن حذف خدمة بعد إصدار الفاتورة؛ استخدم إشعاراً دائناً",
                    "INVOICE_IMMUTABLE");
            }

            var invoiceItem = invoice?.InvoiceItems.FirstOrDefault(item => item.ReservationServiceID == rsId);
            if (invoice != null && invoiceItem == null)
            {
                var service = await _unitOfWork.Services.GetByIdAsync(resService.ServiceID);
                var matches = invoice.InvoiceItems.Where(item => !item.ReservationServiceID.HasValue &&
                    item.ItemName == service?.ServiceName && item.Quantity == resService.Quantity &&
                    item.UnitPrice == resService.UnitPrice).ToList();
                if (matches.Count != 1)
                    return ServiceResult.Failure("تعذر تحديد بند الخدمة التاريخي؛ راجع بنود الفاتورة", "INVOICE_SERVICE_LINK_MISSING");
                invoiceItem = matches[0];
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.ServicesAmount -= resService.TotalPrice;
                reservation.TotalAmount -= resService.TotalPrice;
                reservation.AmountInBaseCurrency = decimal.Round(
                    reservation.TotalAmount * reservation.ExchangeRateUsed,
                    2,
                    MidpointRounding.AwayFromZero);
                reservation.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ReservationServices.Remove(resService);
                reservation.ReservationServices.Remove(resService);
                _unitOfWork.BoothReservations.Update(reservation);

                // إزالة بند الفاتورة المقابل للخدمة المحذوفة وإعادة احتساب قيم الفاتورة
                if (invoice != null && invoiceItem != null)
                {
                    _unitOfWork.InvoiceItems.Remove(invoiceItem);
                    invoice.InvoiceItems.Remove(invoiceItem);

                    invoice.SubTotal = invoice.InvoiceItems.Sum(item => item.TotalPrice);
                    invoice.TaxAmount = InvoiceFinancialSummary.Round(invoice.SubTotal * (invoice.TaxRate / 100));
                    invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
                    invoice.TotalAmountInBaseCurrency = decimal.Round(
                        invoice.TotalAmount * invoice.ExchangeRateUsed,
                        2,
                        MidpointRounding.AwayFromZero);
                    _unitOfWork.Invoices.Update(invoice);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult.Failure("تعذر حذف الخدمة من الحجز.", "REMOVE_SERVICE_FAILED");
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

        private static bool HasUnsettledPayments(Invoice? invoice) => invoice != null &&
            invoice.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount) >
            invoice.CreditNotes.Where(n => n.Type == CreditNoteType.Refund).Sum(n => n.Amount);
    }
}
