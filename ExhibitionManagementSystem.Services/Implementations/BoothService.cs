using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using LayoutPoint = ExhibitionManagementSystem.Services.Common.DrawingPoint;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class BoothService : IBoothService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BoothService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IList<BoothDto>>> GetByHallAsync(int tenantId, int hallId)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<IList<BoothDto>>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var booths = await _unitOfWork.Booths.AsQueryable()
                .Include(b => b.Hall)
                .Where(b => b.HallID == hallId)
                .ToListAsync();
            var dtos = _mapper.Map<IList<BoothDto>>(booths);
            return ServiceResult<IList<BoothDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<BoothSummaryDto>>> GetAvailableAsync(int tenantId, int hallId, int exhibitionId)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<IList<BoothSummaryDto>>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<IList<BoothSummaryDto>>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (hall.VenueID != exhibition.VenueID)
            {
                return ServiceResult<IList<BoothSummaryDto>>.Failure(
                    "القاعة لا تتبع موقع المعرض المحدد",
                    "HALL_NOT_IN_EXHIBITION_VENUE");
            }

            var booths = await _unitOfWork.Booths.FindAsync(b =>
                b.HallID == hallId && b.Status != BoothStatus.Maintenance);

            var mergedBoothIds = await _unitOfWork.BoothMergeItems.AsQueryable()
                .Where(i => i.ExhibitionID == exhibitionId).Select(i => i.BoothID).ToHashSetAsync();

            var reservedBoothIds = await _unitOfWork.BoothReservations.AsQueryable()
                .Where(r => r.ExhibitionID == exhibitionId
                            && r.BoothID.HasValue
                            && r.Status != ReservationStatus.Cancelled)
                .Select(r => r.BoothID!.Value)
                .ToHashSetAsync();

            var availableBooths = booths.Where(b => !reservedBoothIds.Contains(b.BoothID) &&
                !mergedBoothIds.Contains(b.BoothID)).ToList();

            var dtos = _mapper.Map<IList<BoothSummaryDto>>(availableBooths);
            foreach (var dto in dtos)
            {
                dto.Status = BoothStatus.Available.ToString();
            }
            return ServiceResult<IList<BoothSummaryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<BoothDto>> GetByIdAsync(int tenantId, int boothId)
        {
            var booth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(boothId, b => b.Hall, b => b.Hall.Venue);
            if (booth == null || booth.Hall == null || booth.Hall.Venue == null || booth.Hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<BoothDto>.Failure("الكشك غير موجود", "BOOTH_NOT_FOUND");
            }

            var dto = _mapper.Map<BoothDto>(booth);
            return ServiceResult<BoothDto>.Success(dto);
        }

        public async Task<ServiceResult<BoothDto>> CreateAsync(int tenantId, BoothCreateDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await CreateCoreAsync(tenantId, dto);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult<BoothDto>.Failure("تعارض إنشاء الجناح مع عملية أخرى", "FLOOR_PLAN_CONFLICT"); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 1205 or 1222)
            { return ServiceResult<BoothDto>.Failure("تعارض إنشاء الجناح مع عملية أخرى", "FLOOR_PLAN_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<BoothDto>> CreateCoreAsync(int tenantId, BoothCreateDto dto)
        {
            dto.Width = RoundDimension(dto.Width); dto.Height = RoundDimension(dto.Height);
            dto.PosX = RoundDimension(dto.PosX); dto.PosY = RoundDimension(dto.PosY);
            dto.RotationAngle = dto.RotationAngle.HasValue ? NormalizeAngle(decimal.Round(dto.RotationAngle.Value, 2)) : null;
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(dto.HallID, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<BoothDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var boothNumber = dto.BoothNumber.Trim();
            if (string.IsNullOrWhiteSpace(boothNumber))
                return ServiceResult<BoothDto>.Failure("رقم الجناح مطلوب", "BOOTH_NUMBER_REQUIRED");
            if (dto.OriginalAreaSqM <= 0 || dto.Width <= 0 || dto.Height <= 0)
                return ServiceResult<BoothDto>.Failure("مساحة وأبعاد الجناح يجب أن تكون أكبر من صفر", "INVALID_BOOTH_DIMENSIONS");
            if (!EnumInput.TryParse<BoothType>(dto.BoothType, out var boothType))
                return ServiceResult<BoothDto>.Failure("فئة تسعير الجناح غير صالحة", "INVALID_BOOTH_TYPE");
            if (!string.IsNullOrWhiteSpace(dto.ShapeType) &&
                !EnumInput.TryParse<BoothShapeType>(dto.ShapeType, out _))
                return ServiceResult<BoothDto>.Failure("شكل الجناح غير صالح", "INVALID_BOOTH_SHAPE");
            if (await _unitOfWork.Booths.ExistsAsync(existing =>
                    existing.HallID == dto.HallID && existing.BoothNumber == boothNumber))
                return ServiceResult<BoothDto>.Failure("رقم الجناح مستخدم بالفعل داخل القاعة", "DUPLICATE_BOOTH_NUMBER");

            var assignedRuleError = await ValidateAssignedRuleAsync(tenantId, dto.AssignedPriceRuleID);
            if (assignedRuleError != null)
                return ServiceResult<BoothDto>.Failure(assignedRuleError.Value.Message, assignedRuleError.Value.Code);

            var booth = _mapper.Map<Booth>(dto);
            booth.TenantID = tenantId;
            booth.BoothNumber = boothNumber;
            booth.BoothType = boothType;
            if (dto.Width.HasValue && dto.Height.HasValue)
            {
                try
                { booth.OriginalAreaSqM = decimal.Round(BoothDrawingGeometry.Area(BoothDrawingGeometry.BoothPoints(HallDrawingSafety.Item(booth))), 2, MidpointRounding.AwayFromZero); }
                catch (Exception ex) when (ex is JsonException or InvalidOperationException or OverflowException)
                { return ServiceResult<BoothDto>.Failure("نقاط الجناح أو مساحته غير صالحة", "INVALID_BOOTH_POLYGON"); }
                booth.CurrentAreaSqM = booth.OriginalAreaSqM;
            }
            else if (booth.ShapeType == BoothShapeType.Polygon)
                return ServiceResult<BoothDto>.Failure("أبعاد الجناح المضلع مطلوبة", "INVALID_BOOTH_DIMENSIONS");
            if (booth.CurrentAreaSqM <= 0 || booth.CurrentAreaSqM > 99999999.99m)
                return ServiceResult<BoothDto>.Failure("مساحة الجناح غير صالحة", "INVALID_BOOTH_DIMENSIONS");
            var existing = await _unitOfWork.Booths.AsQueryable().AsNoTracking().Where(b => b.HallID == hall.HallID).ToListAsync();
            var geometryError = HallDrawingSafety.Validate(existing.Append(booth), hall.Venue.SitePlanJSON,
                hall.HallID, hall.FloorPlanWidth, hall.FloorPlanHeight, hall.FloorPlanJSON);
            if (geometryError != null)
                return ServiceResult<BoothDto>.Failure(geometryError.Value.Message, geometryError.Value.Code);
            await _unitOfWork.Booths.AddAsync(booth);
            await _unitOfWork.SaveChangesAsync();

            var createdBooth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(booth.BoothID, b => b.Hall);
            var resultDto = _mapper.Map<BoothDto>(createdBooth ?? booth);
            return ServiceResult<BoothDto>.Success(resultDto);
        }

        public async Task<ServiceResult<BoothDto>> UpdateAsync(int tenantId, int boothId, BoothUpdateDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try { return await UpdateCoreAsync(tenantId, boothId, dto); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 1205 or 1222)
            { return ServiceResult<BoothDto>.Failure("تعارض تعديل الجناح مع عملية أخرى", "FLOOR_PLAN_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<BoothDto>> UpdateCoreAsync(int tenantId, int boothId, BoothUpdateDto dto)
        {
            dto.Width = RoundDimension(dto.Width); dto.Height = RoundDimension(dto.Height);
            dto.PosX = RoundDimension(dto.PosX); dto.PosY = RoundDimension(dto.PosY);
            dto.RotationAngle = dto.RotationAngle.HasValue ? NormalizeAngle(decimal.Round(dto.RotationAngle.Value, 2)) : null;
            var booth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(boothId, b => b.Hall, b => b.Hall.Venue);
            if (booth == null || booth.Hall == null || booth.Hall.Venue == null || booth.Hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<BoothDto>.Failure("الكشك غير موجود", "BOOTH_NOT_FOUND");
            }

            var boothNumber = dto.BoothNumber.Trim();
            if (string.IsNullOrWhiteSpace(boothNumber))
                return ServiceResult<BoothDto>.Failure("رقم الجناح مطلوب", "BOOTH_NUMBER_REQUIRED");
            if (!EnumInput.TryParse<BoothStatus>(dto.Status, out _))
                return ServiceResult<BoothDto>.Failure("حالة الجناح غير صالحة", "INVALID_BOOTH_STATUS");
            if (!string.IsNullOrWhiteSpace(dto.BoothType) &&
                !EnumInput.TryParse<BoothType>(dto.BoothType, out _))
                return ServiceResult<BoothDto>.Failure("فئة تسعير الجناح غير صالحة", "INVALID_BOOTH_TYPE");
            if (!string.IsNullOrWhiteSpace(dto.ShapeType) &&
                !EnumInput.TryParse<BoothShapeType>(dto.ShapeType, out _))
                return ServiceResult<BoothDto>.Failure("شكل الجناح غير صالح", "INVALID_BOOTH_SHAPE");
            if (dto.OriginalAreaSqM <= 0 || dto.Width <= 0 || dto.Height <= 0)
                return ServiceResult<BoothDto>.Failure("مساحة وأبعاد الجناح يجب أن تكون أكبر من صفر", "INVALID_BOOTH_DIMENSIONS");
            if (await _unitOfWork.Booths.ExistsAsync(existing =>
                    existing.HallID == booth.HallID && existing.BoothID != boothId && existing.BoothNumber == boothNumber))
                return ServiceResult<BoothDto>.Failure("رقم الجناح مستخدم بالفعل داخل القاعة", "DUPLICATE_BOOTH_NUMBER");

            var assignedRuleError = await ValidateAssignedRuleAsync(tenantId, dto.AssignedPriceRuleID);
            if (assignedRuleError != null)
                return ServiceResult<BoothDto>.Failure(assignedRuleError.Value.Message, assignedRuleError.Value.Code);

            var finalWidth = dto.Width ?? booth.Width;
            var finalHeight = dto.Height ?? booth.Height;
            var finalShape = string.IsNullOrWhiteSpace(dto.ShapeType) ? booth.ShapeType ?? BoothShapeType.Rect
                : Enum.Parse<BoothShapeType>(dto.ShapeType, true);
            var finalPolygon = string.IsNullOrWhiteSpace(dto.ShapePolygonJSON) && finalShape == booth.ShapeType
                ? booth.ShapePolygonJSON : dto.ShapePolygonJSON;
            var finalArea = dto.OriginalAreaSqM ?? booth.CurrentAreaSqM;
            if (finalShape == BoothShapeType.Polygon && (!finalWidth.HasValue || !finalHeight.HasValue))
                return ServiceResult<BoothDto>.Failure("أبعاد الجناح المضلع مطلوبة", "INVALID_BOOTH_DIMENSIONS");
            if (finalWidth.HasValue && finalHeight.HasValue)
            {
                try
                {
                    finalArea = decimal.Round(BoothDrawingGeometry.Area(BoothDrawingGeometry.BoothPoints(new BoothLayoutItemDto
                    { Width = finalWidth.Value, Height = finalHeight.Value, ShapeType = finalShape.ToString(), ShapePolygonJSON = finalPolygon })),
                        2, MidpointRounding.AwayFromZero);
                }
                catch (Exception ex) when (ex is JsonException or InvalidOperationException or OverflowException)
                { return ServiceResult<BoothDto>.Failure("نقاط الجناح أو مساحته غير صالحة", "INVALID_BOOTH_POLYGON"); }
            }
            if (finalArea <= 0 || finalArea > 99999999.99m)
                return ServiceResult<BoothDto>.Failure("مساحة الجناح يجب أن تكون موجبة", "INVALID_BOOTH_DIMENSIONS");

            var affectsPricing = finalArea != booth.CurrentAreaSqM
                || (dto.Width.HasValue && dto.Width != booth.Width) || (dto.Height.HasValue && dto.Height != booth.Height)
                || (dto.PosX.HasValue && dto.PosX != booth.PosX) || (dto.PosY.HasValue && dto.PosY != booth.PosY)
                || (dto.RotationAngle.HasValue && dto.RotationAngle != booth.RotationAngle)
                || finalShape != booth.ShapeType || finalPolygon != booth.ShapePolygonJSON
                || (!string.IsNullOrWhiteSpace(dto.BoothType) && !dto.BoothType.Equals(booth.BoothType.ToString(), StringComparison.OrdinalIgnoreCase))
                || dto.AssignedPriceRuleID != booth.AssignedPriceRuleID;
            if (affectsPricing && await IsPricingLockedAsync(boothId))
                return ServiceResult<BoothDto>.Failure("لا يمكن تعديل مساحة أو تسعير جناح مرتبط بحجز أو دمج؛ فك الدمج أولاً", "BOOTH_GEOMETRY_LOCKED");

            var candidate = new Booth
            {
                BoothID = boothId, BoothNumber = boothNumber, HallID = booth.HallID,
                Width = finalWidth, Height = finalHeight, PosX = dto.PosX ?? booth.PosX, PosY = dto.PosY ?? booth.PosY,
                RotationAngle = dto.RotationAngle ?? booth.RotationAngle, ShapeType = finalShape, ShapePolygonJSON = finalPolygon
            };
            var siblings = await _unitOfWork.Booths.AsQueryable().AsNoTracking().Where(b => b.HallID == booth.HallID && b.BoothID != boothId).ToListAsync();
            var geometryError = HallDrawingSafety.Validate(siblings.Append(candidate), booth.Hall.Venue.SitePlanJSON,
                booth.HallID, booth.Hall.FloorPlanWidth, booth.Hall.FloorPlanHeight, booth.Hall.FloorPlanJSON);
            if (geometryError != null)
                return ServiceResult<BoothDto>.Failure(geometryError.Value.Message, geometryError.Value.Code);

            _mapper.Map(dto, booth);
            booth.Width = candidate.Width; booth.Height = candidate.Height;
            booth.PosX = candidate.PosX; booth.PosY = candidate.PosY; booth.RotationAngle = candidate.RotationAngle;
            booth.ShapeType = finalShape; booth.ShapePolygonJSON = finalPolygon;
            booth.OriginalAreaSqM = booth.CurrentAreaSqM = finalArea;
            booth.BoothNumber = boothNumber;
            if (!string.IsNullOrWhiteSpace(dto.ShapeType))
                booth.ShapeType = Enum.Parse<BoothShapeType>(dto.ShapeType, true);
            if (!string.IsNullOrWhiteSpace(dto.BoothType))
                booth.BoothType = Enum.Parse<BoothType>(dto.BoothType, true);
            booth.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Booths.Update(booth);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var updatedBooth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(booth.BoothID, b => b.Hall, b => b.Hall.Venue);
            var resultDto = _mapper.Map<BoothDto>(updatedBooth ?? booth);
            return ServiceResult<BoothDto>.Success(resultDto);
        }

        public async Task<ServiceResult<BoothLayoutSaveResultDto>> SaveLayoutAsync(
            int tenantId,
            string userId,
            int hallId,
            BoothLayoutSaveDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try { return await SaveLayoutCoreAsync(tenantId, userId, hallId, dto); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 1205 or 1222)
            { return ServiceResult<BoothLayoutSaveResultDto>.Failure("تعارض حفظ المخطط مع عملية أخرى", "FLOOR_PLAN_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<BoothLayoutSaveResultDto>> SaveLayoutCoreAsync(
            int tenantId, string userId, int hallId, BoothLayoutSaveDto dto)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<BoothLayoutSaveResultDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            if ((dto.ExpectedFloorPlanJSON is not null && dto.ExpectedFloorPlanJSON != (hall.FloorPlanJSON ?? string.Empty)) ||
                (dto.ExpectedSitePlanVersion.HasValue && dto.ExpectedSitePlanVersion.Value != hall.Venue.SitePlanVersion))
                return ServiceResult<BoothLayoutSaveResultDto>.Failure("تغير مخطط القاعة أو الموقع. أعد تحميل أحدث نسخة قبل الحفظ.", "FLOOR_PLAN_CONFLICT");
            (decimal Width, decimal Height, IReadOnlyList<LayoutPoint> Points) boundary;
            try { boundary = BoothDrawingGeometry.HallBoundary(hall.Venue.SitePlanJSON, hallId, hall.FloorPlanWidth ?? 0, hall.FloorPlanHeight ?? 0); }
            catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
            { return ServiceResult<BoothLayoutSaveResultDto>.Failure("حدود القاعة غير صالحة.", "INVALID_HALL_OUTLINE"); }
            if (boundary.Width <= 0 || boundary.Height <= 0)
                return ServiceResult<BoothLayoutSaveResultDto>.Failure("حدد أبعاد القاعة أو ارسم حدودها في مصمم الموقع أولاً.", "HALL_DIMENSIONS_REQUIRED");

            if (dto.Booths.Count > 2000)
            {
                return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                    "عدد الأجنحة في المخطط يتجاوز الحد المسموح.",
                    "LAYOUT_TOO_LARGE");
            }

            var duplicateNumber = dto.Booths
                .GroupBy(item => item.BoothNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
            if (duplicateNumber != null)
            {
                return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                    "يجب أن يكون لكل جناح رقم فريد وغير فارغ داخل القاعة.",
                    "DUPLICATE_BOOTH_NUMBER");
            }

            var incomingIds = dto.Booths.Where(item => item.BoothID.HasValue)
                .Select(item => item.BoothID!.Value)
                .ToList();
            var deletedIds = dto.DeletedBoothIDs.Distinct().ToList();
            if (incomingIds.Count != incomingIds.Distinct().Count() || incomingIds.Intersect(deletedIds).Any())
            {
                return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                    "طلب حفظ المخطط يحتوي على معرفات أجنحة متعارضة.",
                    "INVALID_LAYOUT_IDS");
            }

            var existingBooths = await _unitOfWork.Booths.AsQueryable()
                .Where(booth => booth.HallID == hallId)
                .ToListAsync();
            var existingIds = existingBooths.Select(booth => booth.BoothID).ToHashSet();
            var accountedIds = incomingIds.Concat(deletedIds).ToHashSet();
            if (incomingIds.Any(id => !existingIds.Contains(id)) ||
                deletedIds.Any(id => !existingIds.Contains(id)) ||
                existingIds.Any(id => !accountedIds.Contains(id)))
            {
                return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                    "تغيرت بيانات الأجنحة منذ فتح المصمم. أعد تحميل القاعة ثم حاول مجدداً.",
                    "LAYOUT_STALE");
            }

            var assignedRuleIds = dto.Booths
                .Where(item => item.AssignedPriceRuleID.HasValue)
                .Select(item => item.AssignedPriceRuleID!.Value)
                .Distinct()
                .ToList();
            if (assignedRuleIds.Count > 0)
            {
                var ownedRuleIds = await _unitOfWork.BoothPriceRules.AsQueryable()
                    .Where(rule => rule.TenantID == tenantId && assignedRuleIds.Contains(rule.RuleID))
                    .Select(rule => rule.RuleID)
                    .ToListAsync();
                if (ownedRuleIds.Count != assignedRuleIds.Count)
                {
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        "يحتوي المخطط على قاعدة تسعير غير موجودة أو لا تتبع هذا المستأجر.",
                        "PRICING_RULE_NOT_FOUND");
                }
            }

            foreach (var deletedId in deletedIds)
            {
                if (await _unitOfWork.BoothMergeItems.ExistsAsync(i => i.BoothID == deletedId))
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure("يجب فك دمج الجناح قبل حذفه", "BOOTH_IS_MERGED");
                var hasReservations = await _unitOfWork.BoothReservations.ExistsAsync(reservation =>
                    reservation.BoothID == deletedId && reservation.Status != ReservationStatus.Cancelled);
                if (hasReservations)
                {
                    var number = existingBooths.First(booth => booth.BoothID == deletedId).BoothNumber;
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        $"لا يمكن حذف الجناح {number} لوجود حجوزات نشطة عليه.",
                        "BOOTH_HAS_RESERVATIONS");
                }
            }

            var hallWidth = boundary.Width;
            var hallHeight = boundary.Height;
            IReadOnlyList<LayoutObstacle> obstacles;
            try
            {
                obstacles = ParseObstacles(dto.FloorPlanJSON);
            }
            catch (Exception error) when (error is JsonException or InvalidOperationException)
            {
                return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                    "بيانات العناصر المعمارية في المخطط غير صالحة.",
                    "INVALID_FLOOR_PLAN");
            }

            foreach (var obstacle in obstacles)
            {
                if (GetCorners(obstacle).Any(point =>
                    point.X < -0.001m || point.Y < -0.001m ||
                    point.X > hallWidth + 0.001m || point.Y > hallHeight + 0.001m))
                {
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        $"العنصر المعماري {obstacle.Label} يقع خارج حدود القاعة.",
                        "FLOOR_ELEMENT_OUT_OF_BOUNDS");
                }
            }

            var geometry = new Dictionary<BoothLayoutItemDto, IReadOnlyList<LayoutPoint>>();
            IReadOnlyList<IReadOnlyList<LayoutPoint>> siteObstacles;
            try
            {
                foreach (var item in dto.Booths)
                {
                    item.PosX = decimal.Round(item.PosX, 2, MidpointRounding.AwayFromZero);
                    item.PosY = decimal.Round(item.PosY, 2, MidpointRounding.AwayFromZero);
                    item.Width = decimal.Round(item.Width, 2, MidpointRounding.AwayFromZero);
                    item.Height = decimal.Round(item.Height, 2, MidpointRounding.AwayFromZero);
                    item.RotationAngle = NormalizeAngle(decimal.Round(item.RotationAngle, 2, MidpointRounding.AwayFromZero));
                    geometry[item] = GetCorners(item);
                }
                siteObstacles = BoothDrawingGeometry.SiteObstacles(hall.Venue.SitePlanJSON, hallId, boundary.Points);
            }
            catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
            { return ServiceResult<BoothLayoutSaveResultDto>.Failure("حدود جناح متعدد الزوايا غير صالحة.", "INVALID_BOOTH_POLYGON"); }

            foreach (var item in dto.Booths)
            {
                if (item.Width <= 0 || item.Height <= 0 || item.PosX < 0 || item.PosY < 0)
                {
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        $"أبعاد أو إحداثيات الجناح {item.BoothNumber} غير صالحة.",
                        "INVALID_BOOTH_GEOMETRY");
                }

                if (!EnumInput.TryParse<BoothShapeType>(item.ShapeType, out _))
                {
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        $"شكل الجناح {item.BoothNumber} غير مدعوم.",
                        "INVALID_BOOTH_SHAPE");
                }

                if (!EnumInput.TryParse<BoothType>(item.BoothType, out _))
                {
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        $"فئة تسعير الجناح {item.BoothNumber} غير صالحة.",
                        "INVALID_BOOTH_TYPE");
                }

                if (geometry[item].Any(point =>
                    point.X < -0.001m || point.Y < -0.001m ||
                    point.X > hallWidth + 0.001m || point.Y > hallHeight + 0.001m))
                {
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        $"الجناح {item.BoothNumber} يقع خارج حدود القاعة.",
                        "BOOTH_OUT_OF_BOUNDS");
                }

                var boothCorners = geometry[item];
                if (!BoothDrawingGeometry.Inside(boothCorners, boundary.Points))
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure($"الجناح {item.BoothNumber} خارج الحدود الفعلية للقاعة.", "BOOTH_OUT_OF_BOUNDS");
                if (siteObstacles.Any(obstacle => PolygonsOverlap(boothCorners, obstacle)))
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure($"الجناح {item.BoothNumber} يتداخل مع جدار أو عمود القاعة.", "BOOTH_BLOCKS_ACCESS_AREA");
                var blockingObstacle = obstacles.FirstOrDefault(obstacle =>
                    PolygonsOverlap(boothCorners, GetCorners(obstacle)));
                if (blockingObstacle != null)
                {
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                        $"الجناح {item.BoothNumber} يتداخل مع {blockingObstacle.Label}.",
                        "BOOTH_BLOCKS_ACCESS_AREA");
                }
            }

            for (var firstIndex = 0; firstIndex < dto.Booths.Count; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < dto.Booths.Count; secondIndex++)
                {
                    if (PolygonsOverlap(geometry[dto.Booths[firstIndex]], geometry[dto.Booths[secondIndex]]))
                    {
                        return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                            $"يوجد تداخل بين الجناحين {dto.Booths[firstIndex].BoothNumber} و{dto.Booths[secondIndex].BoothNumber}.",
                            "BOOTH_COLLISION");
                    }
                }
            }

            var byId = existingBooths.ToDictionary(booth => booth.BoothID);
            // Validate every edit before mutating any tracked entity.
            foreach (var item in dto.Booths.Where(i => i.BoothID.HasValue))
            {
                var current = byId[item.BoothID!.Value];
                if ((decimal.Round(BoothDrawingGeometry.Area(geometry[item]), 2, MidpointRounding.AwayFromZero) != current.CurrentAreaSqM
                    || item.Width != current.Width || item.Height != current.Height
                    || item.PosX != current.PosX || item.PosY != current.PosY || item.RotationAngle != (current.RotationAngle ?? 0)
                    || !item.BoothType.Equals(current.BoothType.ToString(), StringComparison.OrdinalIgnoreCase)
                    || item.AssignedPriceRuleID != current.AssignedPriceRuleID
                    || !item.ShapeType.Equals(current.ShapeType.ToString(), StringComparison.OrdinalIgnoreCase)
                    || item.ShapePolygonJSON != current.ShapePolygonJSON)
                    && await IsPricingLockedAsync(current.BoothID))
                    return ServiceResult<BoothLayoutSaveResultDto>.Failure($"الجناح {current.BoothNumber} مرتبط بحجز أو دمج ولا يمكن تعديل مساحته أو تسعيره", "BOOTH_GEOMETRY_LOCKED");
            }
            foreach (var item in dto.Booths)
            {
                Booth booth;
                if (item.BoothID.HasValue)
                {
                    booth = byId[item.BoothID.Value];
                }
                else
                {
                    booth = new Booth
                    {
                        TenantID = tenantId,
                        HallID = hallId,
                        Status = BoothStatus.Available
                    };
                    await _unitOfWork.Booths.AddAsync(booth);
                }

                var area = decimal.Round(BoothDrawingGeometry.Area(geometry[item]), 2, MidpointRounding.AwayFromZero);
                booth.BoothNumber = item.BoothNumber.Trim();
                booth.PosX = decimal.Round(item.PosX, 2, MidpointRounding.AwayFromZero);
                booth.PosY = decimal.Round(item.PosY, 2, MidpointRounding.AwayFromZero);
                booth.Width = decimal.Round(item.Width, 2, MidpointRounding.AwayFromZero);
                booth.Height = decimal.Round(item.Height, 2, MidpointRounding.AwayFromZero);
                booth.RotationAngle = NormalizeAngle(item.RotationAngle);
                booth.ShapeType = Enum.Parse<BoothShapeType>(item.ShapeType, true);
                booth.BoothType = Enum.Parse<BoothType>(item.BoothType, true);
                booth.ShapePolygonJSON = item.ShapePolygonJSON ?? string.Empty;
                booth.AssignedPriceRuleID = item.AssignedPriceRuleID;
                booth.OriginalAreaSqM = area;
                booth.CurrentAreaSqM = area;

                if (item.BoothID.HasValue)
                {
                    _unitOfWork.Booths.Update(booth);
                }
            }

            foreach (var deletedId in deletedIds)
            {
                await _unitOfWork.Booths.SoftDeleteAsync(deletedId, userId);
            }

            if (dto.FloorPlanJSON is not null)
            {
                var savedPlan = System.Text.Json.Nodes.JsonNode.Parse(string.IsNullOrWhiteSpace(dto.FloorPlanJSON) ? "{}" : dto.FloorPlanJSON) as System.Text.Json.Nodes.JsonObject ?? new System.Text.Json.Nodes.JsonObject();
                savedPlan["layoutRevision"] = Guid.NewGuid().ToString("N");
                hall.FloorPlanJSON = savedPlan.ToJsonString();
                _unitOfWork.Halls.Update(hall);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                return ServiceResult<BoothLayoutSaveResultDto>.Failure(
                    "تعذر حفظ مخطط الأجنحة.",
                    "LAYOUT_SAVE_FAILED");
            }

            var saved = await _unitOfWork.Booths.AsQueryable()
                .Include(booth => booth.Hall)
                .Where(booth => booth.HallID == hallId)
                .OrderBy(booth => booth.BoothNumber)
                .ToListAsync();

            return ServiceResult<BoothLayoutSaveResultDto>.Success(new BoothLayoutSaveResultDto
            {
                Booths = _mapper.Map<IList<BoothDto>>(saved),
                SavedAtUtc = DateTime.UtcNow,
                FloorPlanJSON = hall.FloorPlanJSON ?? string.Empty
            });
        }

        private async Task<bool> IsPricingLockedAsync(int boothId) =>
            await _unitOfWork.BoothMergeItems.ExistsAsync(i => i.BoothID == boothId)
            || await _unitOfWork.BoothReservations.ExistsAsync(r => r.BoothID == boothId && r.Status != ReservationStatus.Cancelled);

        public async Task<ServiceResult<BoothMergeDto>> MergeBoothsAsync(int tenantId, string userId, BoothMergeCreateDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try { return await MergeBoothsCoreAsync(tenantId, userId, dto); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 1205 or 1222)
            { return ServiceResult<BoothMergeDto>.Failure("تعارض الدمج مع حجز أو تعديل آخر", "RESERVATION_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<BoothMergeDto>> MergeBoothsCoreAsync(int tenantId, string userId, BoothMergeCreateDto dto)
        {
            if (dto.BoothIDs == null || dto.BoothIDs.Count < 2)
            {
                return ServiceResult<BoothMergeDto>.Failure("يجب اختيار كشكين على الأقل لإجراء الدمج", "MINIMUM_BOOTHS_REQUIRED");
            }

            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(dto.HallID, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<BoothMergeDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<BoothMergeDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var booths = await _unitOfWork.Booths.FindAsync(b => dto.BoothIDs.Contains(b.BoothID) && b.HallID == dto.HallID);
            if (hall.VenueID != exhibition.VenueID)
                return ServiceResult<BoothMergeDto>.Failure("القاعة لا تتبع موقع المعرض", "HALL_NOT_IN_EXHIBITION_VENUE");
            if (booths.Count != dto.BoothIDs.Count)
            {
                return ServiceResult<BoothMergeDto>.Failure("بعض الأكشاك غير موجودة أو لا تنتمي لنفس القاعة", "BOOTHS_NOT_FOUND");
            }

            foreach (var b in booths)
            {
                if (b.Status == BoothStatus.Maintenance || await _unitOfWork.BoothMergeItems.ExistsAsync(
                    i => i.BoothID == b.BoothID && i.ExhibitionID == dto.ExhibitionID))
                {
                    return ServiceResult<BoothMergeDto>.Failure($"الكشك {b.BoothNumber} غير متاح للدمج حالياً", "BOOTH_NOT_AVAILABLE");
                }
                var isReserved = await _unitOfWork.BoothReservations.IsBoothReservedAsync(b.BoothID, dto.ExhibitionID);
                if (isReserved)
                {
                    return ServiceResult<BoothMergeDto>.Failure($"الكشك {b.BoothNumber} محجوز في هذا المعرض ولا يمكن دمجه", "BOOTH_RESERVED");
                }
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var totalArea = booths.Sum(b => b.CurrentAreaSqM);
                var boothMerge = new BoothMerge
                {
                    ExhibitionID = dto.ExhibitionID,
                    MergedBoothLabel = dto.MergedBoothLabel,
                    TotalAreaSqM = totalArea,
                    MergedAt = DateTime.UtcNow,
                    MergedByUserId = userId,
                    Notes = dto.Notes
                };

                await _unitOfWork.BoothMerges.AddAsync(boothMerge);
                await _unitOfWork.SaveChangesAsync();

                int seq = 1;
                foreach (var b in booths)
                {
                    var item = new BoothMergeItem
                    {
                        MergeID = boothMerge.MergeID,
                        ExhibitionID = dto.ExhibitionID,
                        BoothID = b.BoothID,
                        SequenceOrder = seq++,
                        OriginalAreaSqM = b.CurrentAreaSqM
                    };
                    boothMerge.MergeItems.Add(item);

                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var fullMerge = await _unitOfWork.BoothMerges.AsQueryable()
                    .Include(m => m.MergeItems)
                        .ThenInclude(mi => mi.Booth)
                            .ThenInclude(b => b.Hall)
                    .FirstOrDefaultAsync(m => m.MergeID == boothMerge.MergeID);

                var resultDto = _mapper.Map<BoothMergeDto>(fullMerge ?? boothMerge);
                return ServiceResult<BoothMergeDto>.Success(resultDto);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothMergeDto>.Failure("تعذر دمج الأكشاك.", "MERGE_FAILED");
            }
        }

        public async Task<ServiceResult> UnmergeBoothsAsync(int tenantId, int mergeId)
        {
            var merge = await _unitOfWork.BoothMerges.AsQueryable()
                .Include(m => m.MergeItems)
                .Include(m => m.Exhibition)
                .FirstOrDefaultAsync(m =>
                    m.MergeID == mergeId &&
                    m.Exhibition.TenantID == tenantId);
            if (merge == null)
            {
                return ServiceResult.Failure("عملية الدمج غير موجودة", "MERGE_NOT_FOUND");
            }

            var isReserved = await _unitOfWork.BoothReservations.IsMergeReservedAsync(mergeId, merge.ExhibitionID);
            if (isReserved)
            {
                return ServiceResult.Failure("لا يمكن فك الدمج لأن الكشك المدمج محجوز حالياً", "MERGE_RESERVED");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var item in merge.MergeItems)
                    _unitOfWork.BoothMergeItems.Remove(item);

                _unitOfWork.BoothMerges.Remove(merge);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult.Failure("تعذر فك دمج الأكشاك.", "UNMERGE_FAILED");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId, string userId, int boothId)
        {
            var booth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(boothId, b => b.Hall, b => b.Hall.Venue);
            if (booth == null || booth.Hall == null || booth.Hall.Venue == null || booth.Hall.Venue.TenantID != tenantId)
            {
                return ServiceResult.Failure("الكشك غير موجود", "BOOTH_NOT_FOUND");
            }

            var hasReservations = await _unitOfWork.BoothReservations.ExistsAsync(r => r.BoothID == boothId && r.Status != ReservationStatus.Cancelled);
            if (hasReservations)
            {
                return ServiceResult.Failure("لا يمكن حذف الكشك لوجود حجوزات نشطة عليه", "BOOTH_HAS_RESERVATIONS");
            }

            if (await _unitOfWork.BoothMergeItems.ExistsAsync(i => i.BoothID == boothId))
                return ServiceResult.Failure("يجب فك دمج الجناح قبل حذفه", "BOOTH_IS_MERGED");

            await _unitOfWork.Booths.SoftDeleteAsync(boothId, userId);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        private static decimal NormalizeAngle(decimal angle)
        {
            var normalized = angle % 360m;
            return normalized < 0 ? normalized + 360m : normalized;
        }

        private static decimal? RoundDimension(decimal? value) => value.HasValue
            ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero) : null;

        private async Task<(string Message, string Code)?> ValidateAssignedRuleAsync(int tenantId, int? ruleId)
        {
            if (!ruleId.HasValue) return null;
            var rule = await _unitOfWork.BoothPriceRules.GetByIdAsync(ruleId.Value);
            return rule == null || rule.TenantID != tenantId
                ? ("قاعدة التسعير المعينة غير موجودة أو لا تتبع هذا المستأجر", "PRICING_RULE_NOT_FOUND")
                : null;
        }

        private static IReadOnlyList<LayoutPoint> GetCorners(BoothLayoutItemDto item)
            => BoothDrawingGeometry.BoothPoints(item);

        private static IReadOnlyList<LayoutPoint> GetCorners(LayoutObstacle item)
            => GetRectangleCorners(item.X, item.Y, item.Width, item.Height, item.Rotation);

        private static IReadOnlyList<LayoutPoint> GetRectangleCorners(
            decimal x,
            decimal y,
            decimal width,
            decimal height,
            decimal rotation)
        {
            var centerX = x + width / 2m;
            var centerY = y + height / 2m;
            var radians = (double)rotation * Math.PI / 180d;
            var cosine = (decimal)Math.Cos(radians);
            var sine = (decimal)Math.Sin(radians);
            var halfWidth = width / 2m;
            var halfHeight = height / 2m;
            var offsets = new[]
            {
                new LayoutPoint(-halfWidth, -halfHeight),
                new LayoutPoint(halfWidth, -halfHeight),
                new LayoutPoint(halfWidth, halfHeight),
                new LayoutPoint(-halfWidth, halfHeight)
            };

            return offsets.Select(offset => new LayoutPoint(
                centerX + offset.X * cosine - offset.Y * sine,
                centerY + offset.X * sine + offset.Y * cosine)).ToArray();
        }

        private static IReadOnlyList<LayoutObstacle> ParseObstacles(string? floorPlanJson)
        {
            if (string.IsNullOrWhiteSpace(floorPlanJson)) return [];
            using var document = JsonDocument.Parse(floorPlanJson);
            if (!document.RootElement.TryGetProperty("elements", out var elements) ||
                elements.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<LayoutObstacle>();
            foreach (var element in elements.EnumerateArray())
            {
                if (!element.TryGetProperty("type", out var typeProperty)) continue;
                var type = typeProperty.GetString();
                if (type is not ("aisle" or "column" or "restricted")) continue;
                if (!TryGetDecimal(element, "x", out var x) ||
                    !TryGetDecimal(element, "y", out var y) ||
                    !TryGetDecimal(element, "width", out var width) ||
                    !TryGetDecimal(element, "height", out var height) ||
                    width <= 0 || height <= 0)
                {
                    throw new JsonException("Invalid obstacle geometry.");
                }

                TryGetDecimal(element, "rotation", out var rotation);
                var label = element.TryGetProperty("label", out var labelProperty)
                    ? labelProperty.GetString()
                    : null;
                result.Add(new LayoutObstacle(
                    x,
                    y,
                    width,
                    height,
                    rotation,
                    string.IsNullOrWhiteSpace(label) ? "منطقة محجوزة" : label));
            }

            return result;
        }

        private static bool TryGetDecimal(JsonElement element, string propertyName, out decimal value)
        {
            value = 0;
            return element.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind == JsonValueKind.Number &&
                   property.TryGetDecimal(out value);
        }

        private static bool PolygonsOverlap(IReadOnlyList<LayoutPoint> first, IReadOnlyList<LayoutPoint> second)
        {
            return BoothDrawingGeometry.Overlap(first, second);
        }

        private static bool HasSeparatingAxis(IReadOnlyList<LayoutPoint> source, IReadOnlyList<LayoutPoint> other)
        {
            for (var index = 0; index < source.Count; index++)
            {
                var current = source[index];
                var next = source[(index + 1) % source.Count];
                var axisX = -(next.Y - current.Y);
                var axisY = next.X - current.X;
                var sourceProjection = Project(source, axisX, axisY);
                var otherProjection = Project(other, axisX, axisY);
                if (sourceProjection.Max <= otherProjection.Min + 0.001m ||
                    otherProjection.Max <= sourceProjection.Min + 0.001m)
                {
                    return true;
                }
            }

            return false;
        }

        private static (decimal Min, decimal Max) Project(
            IReadOnlyList<LayoutPoint> points,
            decimal axisX,
            decimal axisY)
        {
            var values = points.Select(point => point.X * axisX + point.Y * axisY).ToArray();
            return (values.Min(), values.Max());
        }


        private sealed record LayoutObstacle(
            decimal X,
            decimal Y,
            decimal Width,
            decimal Height,
            decimal Rotation,
            string Label);
    }
}
