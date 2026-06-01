using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

            var booths = await _unitOfWork.Booths.FindAsync(b => b.HallID == hallId);
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

            var booths = await _unitOfWork.Booths.FindAsync(b => b.HallID == hallId && b.Status == BoothStatus.Available);

            var reservedBoothIds = await _unitOfWork.BoothReservations.AsQueryable()
                .Where(r => r.ExhibitionID == exhibitionId
                            && r.BoothID.HasValue
                            && r.Status != ReservationStatus.Cancelled)
                .Select(r => r.BoothID!.Value)
                .ToHashSetAsync();

            var availableBooths = booths.Where(b => !reservedBoothIds.Contains(b.BoothID)).ToList();

            var dtos = _mapper.Map<IList<BoothSummaryDto>>(availableBooths);
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
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(dto.HallID, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<BoothDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var booth = _mapper.Map<Booth>(dto);
            await _unitOfWork.Booths.AddAsync(booth);
            await _unitOfWork.SaveChangesAsync();

            var createdBooth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(booth.BoothID, b => b.Hall);
            var resultDto = _mapper.Map<BoothDto>(createdBooth ?? booth);
            return ServiceResult<BoothDto>.Success(resultDto);
        }

        public async Task<ServiceResult<BoothDto>> UpdateAsync(int tenantId, int boothId, BoothUpdateDto dto)
        {
            var booth = await _unitOfWork.Booths.GetByIdWithIncludesAsync(boothId, b => b.Hall, b => b.Hall.Venue);
            if (booth == null || booth.Hall == null || booth.Hall.Venue == null || booth.Hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<BoothDto>.Failure("الكشك غير موجود", "BOOTH_NOT_FOUND");
            }

            _mapper.Map(dto, booth);
            booth.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Booths.Update(booth);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<BoothDto>(booth);
            return ServiceResult<BoothDto>.Success(resultDto);
        }

        public async Task<ServiceResult<BoothMergeDto>> MergeBoothsAsync(int tenantId, string userId, BoothMergeCreateDto dto)
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
            if (booths.Count != dto.BoothIDs.Count)
            {
                return ServiceResult<BoothMergeDto>.Failure("بعض الأكشاك غير موجودة أو لا تنتمي لنفس القاعة", "BOOTHS_NOT_FOUND");
            }

            foreach (var b in booths)
            {
                if (b.Status != BoothStatus.Available || b.IsMerged || b.MergeID != null)
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
                var totalArea = booths.Sum(b => b.OriginalAreaSqM);
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
                        BoothID = b.BoothID,
                        SequenceOrder = seq++,
                        OriginalAreaSqM = b.OriginalAreaSqM
                    };
                    boothMerge.MergeItems.Add(item);

                    b.IsMerged = true;
                    b.MergeID = boothMerge.MergeID;
                    b.Status = BoothStatus.Merged;
                    _unitOfWork.Booths.Update(b);
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
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BoothMergeDto>.Failure($"فشل دمج الأكشاك: {ex.Message}", "MERGE_FAILED");
            }
        }

        public async Task<ServiceResult> UnmergeBoothsAsync(int tenantId, int mergeId)
        {
            var merge = await _unitOfWork.BoothMerges.GetByIdWithIncludesAsync(mergeId, m => m.MergeItems);
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
                var booths = await _unitOfWork.Booths.FindAsync(b => b.MergeID == mergeId);
                foreach (var b in booths)
                {
                    b.IsMerged = false;
                    b.MergeID = null;
                    b.Status = BoothStatus.Available;
                    _unitOfWork.Booths.Update(b);
                }

                _unitOfWork.BoothMerges.Remove(merge);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult.Failure($"فشل فك دمج الأكشاك: {ex.Message}", "UNMERGE_FAILED");
            }
        }
    }
}
