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

        public ReservationService(IUnitOfWork unitOfWork, IMapper mapper, IPricingService pricingService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _pricingService = pricingService;
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

            if (!Enum.TryParse<BoothType>(dto.BoothTypeSelected, true, out var boothType))
            {
                return ServiceResult<BoothReservationDto>.Failure("نوع الكشك غير صالح", "INVALID_BOOTH_TYPE");
            }

            if (!Enum.TryParse<ExhibitorCategory>(dto.ExhibitorCategory, true, out var exhibitorCategory))
            {
                return ServiceResult<BoothReservationDto>.Failure("فئة العارض غير صالحة", "INVALID_EXHIBITOR_CATEGORY");
            }

            var priceResult = await _pricingService.CalculateBoothPriceAsync(tenantId, dto.ExhibitionID, boothType, exhibitorCategory, dto.RequestedAreaSqM);
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
                try
                {
                    exchangeRateUsed = await _unitOfWork.ExchangeRates.ConvertAsync(dto.CurrencyCode, tenant.BaseCurrency, 1.0m);
                }
                catch (Exception ex)
                {
                    return ServiceResult<BoothReservationDto>.Failure($"لا يمكن حساب سعر الصرف: {ex.Message}", "EXCHANGE_RATE_NOT_FOUND");
                }
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
                await _unitOfWork.CommitTransactionAsync();

                var fullReservation = await _unitOfWork.BoothReservations.AsQueryable()
                    .Include(r => r.Exhibitor)
                    .Include(r => r.Booth)
                    .Include(r => r.Exhibition)
                    .Include(r => r.Currency)
                    .FirstOrDefaultAsync(r => r.ReservationID == reservation.ReservationID);

                var resultDto = _mapper.Map<BoothReservationDto>(fullReservation ?? reservation);
                return ServiceResult<BoothReservationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure($"فشل إنشاء الحجز: {ex.Message}", "RESERVATION_CREATION_FAILED");
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

            if (!Enum.TryParse<ReservationStatus>(dto.Status, true, out var newStatus))
            {
                return ServiceResult<BoothReservationDto>.Failure("حالة الحجز غير صالحة", "INVALID_RESERVATION_STATUS");
            }

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
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure($"فشل تحديث الحجز: {ex.Message}", "RESERVATION_UPDATE_FAILED");
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

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult.Failure($"فشل إلغاء الحجز: {ex.Message}", "CANCEL_FAILED");
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

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return await GetByIdAsync(tenantId, id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothReservationDto>.Failure($"فشل تأكيد الحجز: {ex.Message}", "APPROVE_FAILED");
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

            var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceID);
            if (service == null || service.TenantID != tenantId)
            {
                return ServiceResult<ReservationServiceDto>.Failure("الخدمة غير موجودة", "SERVICE_NOT_FOUND");
            }

            var servicePriceResult = await _pricingService.CalculateServicePriceAsync(tenantId, dto.ServiceID, reservation.ExhibitionID, (int)dto.Quantity);
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
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var mappedDto = _mapper.Map<ReservationServiceDto>(resService);
                mappedDto.ServiceName = service.ServiceName; // Populate navigation field manually if Mapper didn't
                return ServiceResult<ReservationServiceDto>.Success(mappedDto);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<ReservationServiceDto>.Failure($"فشل إضافة الخدمة للحجز: {ex.Message}", "ADD_SERVICE_FAILED");
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.ServicesAmount -= resService.TotalPrice;
                reservation.TotalAmount -= resService.TotalPrice;
                reservation.AmountInBaseCurrency = reservation.TotalAmount * reservation.ExchangeRateUsed;
                reservation.UpdatedAt = DateTime.UtcNow;

                reservation.ReservationServices.Remove(resService);
                _unitOfWork.BoothReservations.Update(reservation);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult.Failure($"فشل حذف الخدمة من الحجز: {ex.Message}", "REMOVE_SERVICE_FAILED");
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
    }
}
