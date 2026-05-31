using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class VisitorService : IVisitorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VisitorService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<PagedResultDto<VisitorDto>>> GetByTenantAsync(int tenantId, int page, int pageSize)
        {
            var query = _unitOfWork.Visitors.AsQueryable()
                .Include(v => v.Tickets)
                .Where(v => v.TenantID == tenantId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(v => v.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IList<VisitorDto>>(items);
            var result = new PagedResultDto<VisitorDto>
            {
                Items = dtos.ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<VisitorDto>>.Success(result);
        }

        public async Task<ServiceResult<IList<VisitorDto>>> SearchAsync(int tenantId, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                var all = await _unitOfWork.Visitors.AsQueryable()
                    .Include(v => v.Tickets)
                    .Where(v => v.TenantID == tenantId)
                    .ToListAsync();
                var allDtos = _mapper.Map<IList<VisitorDto>>(all);
                return ServiceResult<IList<VisitorDto>>.Success(allDtos);
            }

            var visitors = await _unitOfWork.Visitors.SearchAsync(tenantId, term);
            
            // To ensure Tickets count is mapped, reload or join since SearchAsync might not include them.
            var visitorIds = visitors.Select(v => v.VisitorID).ToList();
            var visitorsWithTickets = await _unitOfWork.Visitors.AsQueryable()
                .Include(v => v.Tickets)
                .Where(v => visitorIds.Contains(v.VisitorID))
                .ToListAsync();

            var dtos = _mapper.Map<IList<VisitorDto>>(visitorsWithTickets);
            return ServiceResult<IList<VisitorDto>>.Success(dtos);
        }

        public async Task<ServiceResult<VisitorDto>> GetByIdAsync(int tenantId, int visitorId)
        {
            var visitor = await _unitOfWork.Visitors.AsQueryable()
                .Include(v => v.Tickets)
                .FirstOrDefaultAsync(v => v.VisitorID == visitorId && v.TenantID == tenantId);

            if (visitor == null)
            {
                return ServiceResult<VisitorDto>.Failure("الزائر غير موجود", "VISITOR_NOT_FOUND");
            }

            var dto = _mapper.Map<VisitorDto>(visitor);
            return ServiceResult<VisitorDto>.Success(dto);
        }

        public async Task<ServiceResult<VisitorDto>> RegisterAsync(int tenantId, VisitorCreateDto dto)
        {
            var existing = await _unitOfWork.Visitors.GetByEmailAsync(tenantId, dto.Email);
            if (existing != null)
            {
                return ServiceResult<VisitorDto>.Failure("البريد الإلكتروني مسجل بالفعل", "EMAIL_ALREADY_EXISTS");
            }

            var visitor = _mapper.Map<Visitor>(dto);
            visitor.TenantID = tenantId;
            visitor.RegisteredAt = DateTime.UtcNow;

            await _unitOfWork.Visitors.AddAsync(visitor);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<VisitorDto>(visitor);
            resultDto.TicketsCount = 0;
            return ServiceResult<VisitorDto>.Success(resultDto);
        }

        public async Task<ServiceResult<VisitorRatingDto>> SubmitRatingAsync(int tenantId, int visitorId, int exhibitionId, int rating, string? comment)
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
            if (visitor == null || visitor.TenantID != tenantId)
            {
                return ServiceResult<VisitorRatingDto>.Failure("الزائر غير موجود", "VISITOR_NOT_FOUND");
            }

            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<VisitorRatingDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (rating < 1 || rating > 5)
            {
                return ServiceResult<VisitorRatingDto>.Failure("التقييم يجب أن يكون بين 1 و 5", "INVALID_RATING_SCORE");
            }

            var hasRated = await _unitOfWork.VisitorRatings.HasVisitorRatedAsync(visitorId, exhibitionId);
            if (hasRated)
            {
                return ServiceResult<VisitorRatingDto>.Failure("لقد قمت بتقييم هذا المعرض بالفعل", "DUPLICATE_RATING");
            }

            var visitorRating = new VisitorRating
            {
                VisitorID = visitorId,
                ExhibitionID = exhibitionId,
                Score = (byte)rating,
                Comment = comment ?? string.Empty,
                RatedAt = DateTime.UtcNow
            };

            await _unitOfWork.VisitorRatings.AddAsync(visitorRating);
            await _unitOfWork.SaveChangesAsync();

            var fullRating = await _unitOfWork.VisitorRatings.AsQueryable()
                .Include(r => r.Visitor)
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.RatingID == visitorRating.RatingID);

            var dto = _mapper.Map<VisitorRatingDto>(fullRating ?? visitorRating);
            return ServiceResult<VisitorRatingDto>.Success(dto);
        }

        public async Task<ServiceResult<VisitorRatingSummaryDto>> GetRatingSummaryAsync(int tenantId, int exhibitionId)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<VisitorRatingSummaryDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var ratings = await _unitOfWork.VisitorRatings.AsQueryable()
                .Where(r => r.ExhibitionID == exhibitionId)
                .ToListAsync();

            var totalRatings = ratings.Count;
            var averageRating = totalRatings > 0 ? (decimal)ratings.Average(r => r.Score) : 0m;

            var distribution = new Dictionary<int, int>
            {
                { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
            };

            foreach (var r in ratings)
            {
                if (distribution.ContainsKey(r.Score))
                {
                    distribution[r.Score]++;
                }
            }

            var dto = new VisitorRatingSummaryDto
            {
                ExhibitionID = exhibitionId,
                ExhibitionName = exhibition.Name,
                AverageRating = averageRating,
                TotalRatings = totalRatings,
                RatingDistribution = distribution
            };

            return ServiceResult<VisitorRatingSummaryDto>.Success(dto);
        }
    }
}
