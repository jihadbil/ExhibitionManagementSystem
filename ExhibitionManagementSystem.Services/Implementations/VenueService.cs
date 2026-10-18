using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class VenueService : IVenueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VenueService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IList<VenueDto>>> GetByTenantAsync(int tenantId)
        {
            var venues = await _unitOfWork.Venues.GetByTenantAsync(tenantId);
            var dtos = _mapper.Map<IList<VenueDto>>(venues);
            return ServiceResult<IList<VenueDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<VenueSummaryDto>>> GetSummariesAsync(int tenantId)
        {
            var venues = await _unitOfWork.Venues.GetByTenantAsync(tenantId);
            var dtos = _mapper.Map<IList<VenueSummaryDto>>(venues);
            return ServiceResult<IList<VenueSummaryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<VenueDto>> GetByIdAsync(int tenantId, int venueId)
        {
            // We use includes for Halls to map the HallsCount in mapping profile
            var venue = await _unitOfWork.Venues.GetByIdWithIncludesAsync(venueId, v => v.Halls);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<VenueDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            var dto = _mapper.Map<VenueDto>(venue);
            return ServiceResult<VenueDto>.Success(dto);
        }

        public async Task<ServiceResult<VenueDto>> CreateAsync(int tenantId, VenueCreateDto dto)
        {
            var venue = _mapper.Map<Venue>(dto);
            venue.TenantID = tenantId;

            await _unitOfWork.Venues.AddAsync(venue);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<VenueDto>(venue);
            return ServiceResult<VenueDto>.Success(resultDto);
        }

        public async Task<ServiceResult<VenueDto>> UpdateAsync(int tenantId, int venueId, VenueUpdateDto dto)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(venueId);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<VenueDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            _mapper.Map(dto, venue);
            venue.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Venues.Update(venue);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<VenueDto>(venue);
            return ServiceResult<VenueDto>.Success(resultDto);
        }

        public async Task<ServiceResult<VenueSitePlanDto>> GetSitePlanAsync(int tenantId, int venueId)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(venueId);
            if (venue == null || venue.TenantID != tenantId)
                return ServiceResult<VenueSitePlanDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");

            return ServiceResult<VenueSitePlanDto>.Success(new VenueSitePlanDto
            {
                VenueID = venue.VenueID,
                PlanJSON = venue.SitePlanJSON,
                UpdatedAt = venue.UpdatedAt,
                Version = venue.SitePlanVersion
            });
        }

        public async Task<ServiceResult<VenueSitePlanDto>> SaveSitePlanAsync(
            int tenantId,
            int venueId,
            VenueSitePlanSaveDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await SaveSitePlanCoreAsync(tenantId, venueId, dto);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 1205 or 1222)
            { return ServiceResult<VenueSitePlanDto>.Failure("تعارض المخطط مع حجز أو تعديل آخر", "SITE_PLAN_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<VenueSitePlanDto>> SaveSitePlanCoreAsync(int tenantId, int venueId, VenueSitePlanSaveDto dto)
        {
            if (dto.PlanJSON.Length > 2_000_000)
                return ServiceResult<VenueSitePlanDto>.Failure("مخطط الموقع كبير جداً", "SITE_PLAN_TOO_LARGE");

            var venue = await _unitOfWork.Venues.GetByIdWithIncludesAsync(venueId, item => item.Halls);
            if (venue == null || venue.TenantID != tenantId)
                return ServiceResult<VenueSitePlanDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");

            if (dto.ExpectedVersion.HasValue && venue.SitePlanVersion != dto.ExpectedVersion.Value)
            {
                return ServiceResult<VenueSitePlanDto>.Failure(
                    "تم تعديل المخطط في جلسة أخرى. أعد تحميل المخطط قبل متابعة الحفظ.",
                    "SITE_PLAN_CONFLICT");
            }
            if (!dto.ExpectedVersion.HasValue && !string.IsNullOrWhiteSpace(venue.SitePlanJSON))
            {
                return ServiceResult<VenueSitePlanDto>.Failure(
                    "تم حفظ نسخة أحدث من المخطط في جلسة أخرى. أعد تحميل المخطط قبل متابعة الحفظ.",
                    "SITE_PLAN_CONFLICT");
            }

            try
            {
                using var document = JsonDocument.Parse(dto.PlanJSON);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("bounds", out var bounds)
                    || bounds.ValueKind != JsonValueKind.Object
                    || !bounds.TryGetProperty("width", out var widthProperty)
                    || !bounds.TryGetProperty("height", out var heightProperty)
                    || !widthProperty.TryGetDecimal(out var width)
                    || !heightProperty.TryGetDecimal(out var height)
                    || width <= 0 || height <= 0 || width > 10_000 || height > 10_000
                    || !root.TryGetProperty("halls", out var hallsProperty)
                    || hallsProperty.ValueKind != JsonValueKind.Array)
                {
                    return ServiceResult<VenueSitePlanDto>.Failure("بنية مخطط الموقع غير صالحة", "SITE_PLAN_INVALID");
                }

                var ownedHallIds = venue.Halls.Select(hall => hall.HallID).ToHashSet();
                if (!VenueDrawingValidator.IsValid(root, ownedHallIds, (double)width, (double)height))
                    return ServiceResult<VenueSitePlanDto>.Failure("بنية الرسم أو حدود القاعات غير صالحة", "SITE_PLAN_DRAWING_INVALID");
                foreach (var hallElement in hallsProperty.EnumerateArray())
                {
                    if (hallElement.ValueKind != JsonValueKind.Object
                        || !hallElement.TryGetProperty("hallID", out var hallIdProperty)
                        || !hallIdProperty.TryGetInt32(out var hallId)
                        || !ownedHallIds.Contains(hallId)
                        || !hallElement.TryGetProperty("x", out var hallXProperty)
                        || !hallXProperty.TryGetDecimal(out var hallX)
                        || !hallElement.TryGetProperty("y", out var hallYProperty)
                        || !hallYProperty.TryGetDecimal(out var hallY)
                        || !hallElement.TryGetProperty("width", out var hallWidthProperty)
                        || !hallWidthProperty.TryGetDecimal(out var hallWidth)
                        || !hallElement.TryGetProperty("height", out var hallHeightProperty)
                        || !hallHeightProperty.TryGetDecimal(out var hallHeight)
                        || hallX < 0 || hallY < 0 || hallWidth <= 0 || hallHeight <= 0
                        || hallX + hallWidth > width || hallY + hallHeight > height)
                    {
                        return ServiceResult<VenueSitePlanDto>.Failure("يحتوي المخطط على قاعة غير صالحة أو خارج حدود الموقع", "SITE_PLAN_HALL_INVALID");
                    }
                }

                if (root.TryGetProperty("hallDoors", out var doorsProperty))
                {
                    if (doorsProperty.ValueKind != JsonValueKind.Array)
                        return ServiceResult<VenueSitePlanDto>.Failure("بنية أبواب القاعات غير صالحة", "SITE_PLAN_DOORS_INVALID");

                    var validEdges = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "n", "e", "s", "w" };
                    var doorIds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var doorElement in doorsProperty.EnumerateArray())
                    {
                        if (doorElement.ValueKind != JsonValueKind.Object
                            || !doorElement.TryGetProperty("id", out var idProperty)
                            || idProperty.ValueKind != JsonValueKind.String
                            || string.IsNullOrWhiteSpace(idProperty.GetString())
                            || !doorIds.Add(idProperty.GetString()!)
                            || !doorElement.TryGetProperty("hallID", out var doorHallIdProperty)
                            || !doorHallIdProperty.TryGetInt32(out var doorHallId)
                            || !ownedHallIds.Contains(doorHallId)
                            || !doorElement.TryGetProperty("edge", out var edgeProperty)
                            || edgeProperty.ValueKind != JsonValueKind.String
                            || !validEdges.Contains(edgeProperty.GetString() ?? string.Empty)
                            || !doorElement.TryGetProperty("offset", out var offsetProperty)
                            || !offsetProperty.TryGetDecimal(out var offset)
                            || offset < 0 || offset > 1
                            || !doorElement.TryGetProperty("width", out var doorWidthProperty)
                            || !doorWidthProperty.TryGetDecimal(out var doorWidth)
                            || doorWidth < 0.6m || doorWidth > 12m)
                        {
                            return ServiceResult<VenueSitePlanDto>.Failure("يحتوي المخطط على باب قاعة غير صالح أو غير مرتبط بهذا الموقع", "SITE_PLAN_DOOR_INVALID");
                        }
                    }
                }

                if (root.TryGetProperty("elements", out var elementsProperty))
                {
                    if (elementsProperty.ValueKind != JsonValueKind.Array)
                        return ServiceResult<VenueSitePlanDto>.Failure("بنية عناصر المخطط غير صالحة", "SITE_PLAN_ELEMENTS_INVALID");

                    var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "wall", "emergencyExit", "aisle", "stage", "service", "restricted", "column"
                    };
                    var validLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "structure", "movement", "safety", "services"
                    };
                    var elementIds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var element in elementsProperty.EnumerateArray())
                    {
                        if (element.ValueKind != JsonValueKind.Object
                            || !element.TryGetProperty("id", out var idProperty)
                            || idProperty.ValueKind != JsonValueKind.String
                            || string.IsNullOrWhiteSpace(idProperty.GetString())
                            || !elementIds.Add(idProperty.GetString()!)
                            || !element.TryGetProperty("type", out var typeProperty)
                            || typeProperty.ValueKind != JsonValueKind.String
                            || !validTypes.Contains(typeProperty.GetString() ?? string.Empty)
                            || !element.TryGetProperty("layer", out var layerProperty)
                            || layerProperty.ValueKind != JsonValueKind.String
                            || !validLayers.Contains(layerProperty.GetString() ?? string.Empty)
                            || !element.TryGetProperty("x", out var xProperty)
                            || !xProperty.TryGetDecimal(out var x)
                            || !element.TryGetProperty("y", out var yProperty)
                            || !yProperty.TryGetDecimal(out var y)
                            || !element.TryGetProperty("width", out var elementWidthProperty)
                            || !elementWidthProperty.TryGetDecimal(out var elementWidth)
                            || !element.TryGetProperty("height", out var elementHeightProperty)
                            || !elementHeightProperty.TryGetDecimal(out var elementHeight)
                            || x < 0 || y < 0 || elementWidth <= 0 || elementHeight <= 0
                            || x + elementWidth > width || y + elementHeight > height)
                        {
                            return ServiceResult<VenueSitePlanDto>.Failure("يحتوي المخطط على عنصر غير صالح أو خارج الحدود", "SITE_PLAN_ELEMENT_INVALID");
                        }

                        var type = typeProperty.GetString();
                        if (string.Equals(type, "aisle", StringComparison.OrdinalIgnoreCase)
                            && Math.Min(elementWidth, elementHeight) < 1.2m)
                        {
                            return ServiceResult<VenueSitePlanDto>.Failure("عرض الممرات يجب ألا يقل عن 1.2 متر", "SITE_PLAN_AISLE_INVALID");
                        }
                        if (string.Equals(type, "emergencyExit", StringComparison.OrdinalIgnoreCase)
                            && Math.Max(elementWidth, elementHeight) < 0.9m)
                        {
                            return ServiceResult<VenueSitePlanDto>.Failure("عرض مخرج الطوارئ غير صالح", "SITE_PLAN_EXIT_INVALID");
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return ServiceResult<VenueSitePlanDto>.Failure("مخطط الموقع ليس JSON صالحاً", "SITE_PLAN_INVALID_JSON");
            }
            catch (InvalidOperationException)
            {
                return ServiceResult<VenueSitePlanDto>.Failure("أنواع حقول مخطط الموقع غير صالحة", "SITE_PLAN_INVALID");
            }

            var hallIds = venue.Halls.Select(h => h.HallID).ToList();
            var booths = await _unitOfWork.Booths.AsQueryable().AsNoTracking().Where(b => hallIds.Contains(b.HallID)).ToListAsync();
            foreach (var hall in venue.Halls)
            {
                var error = HallDrawingSafety.Validate(booths.Where(b => b.HallID == hall.HallID), dto.PlanJSON,
                    hall.HallID, hall.FloorPlanWidth, hall.FloorPlanHeight, hall.FloorPlanJSON);
                if (error != null)
                    return ServiceResult<VenueSitePlanDto>.Failure(error.Value.Message, error.Value.Code);
            }

            venue.SitePlanJSON = dto.PlanJSON;
            venue.UpdatedAt = DateTime.UtcNow;
            venue.SitePlanVersion++;
            _unitOfWork.Venues.Update(venue);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<VenueSitePlanDto>.Failure(
                    "تم تعديل المخطط في جلسة أخرى. أعد تحميل المخطط قبل متابعة الحفظ.",
                    "SITE_PLAN_CONFLICT");
            }

            return ServiceResult<VenueSitePlanDto>.Success(new VenueSitePlanDto
            {
                VenueID = venue.VenueID,
                PlanJSON = venue.SitePlanJSON,
                UpdatedAt = venue.UpdatedAt,
                Version = venue.SitePlanVersion
            });
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId, int venueId)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await DeleteCoreAsync(tenantId, venueId);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult.Failure("تعارض حذف الموقع مع عملية أخرى", "VENUE_DELETE_CONFLICT"); }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult.Failure("تعارض حذف الموقع مع عملية أخرى", "VENUE_DELETE_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult> DeleteCoreAsync(int tenantId, int venueId)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(venueId);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            if (await _unitOfWork.Halls.AsQueryable().IgnoreQueryFilters().AnyAsync(h => h.VenueID == venueId)
                || await _unitOfWork.Exhibitions.AsQueryable().IgnoreQueryFilters().AnyAsync(e => e.TenantID == tenantId && e.VenueID == venueId))
                return ServiceResult.Failure("لا يمكن حذف الموقع لوجود قاعات أو معارض مرتبطة به", "VENUE_HAS_DEPENDENCIES");

            await _unitOfWork.Venues.SoftDeleteAsync(venueId, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
    }
}
