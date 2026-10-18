using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExhibitionManagementSystem.Controllers.Booth;

[Route("api/[controller]")]
public class BoothsController : BaseApiController
{
    private readonly IBoothService _boothService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BoothsController(IBoothService boothService, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _boothService = boothService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // GET /api/booths
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<BoothDto>>> GetAll(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] int? hallId,
        [FromQuery] int? exhibitionId,
        [FromQuery] string? status)
    {
        var tenantId = TenantId;
        var query = _unitOfWork.Booths.AsQueryable()
            .Include(b => b.Hall)
            .ThenInclude(h => h.Venue)
            .Where(b => b.Hall.Venue.TenantID == tenantId);
        HashSet<int>? exhibitionReservedBoothIds = null;
        HashSet<int>? exhibitionMergedBoothIds = null;
        Dictionary<int, int>? exhibitionMergeIds = null;

        if (hallId.HasValue)
        {
            query = query.Where(b => b.HallID == hallId.Value);
        }

        if (exhibitionId.HasValue)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId.Value);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return NotFound(new { ErrorMessage = "المعرض غير موجود", ErrorCode = "EXHIBITION_NOT_FOUND" });
            }
            query = query.Where(b => b.Hall.VenueID == exhibition.VenueID);
            exhibitionReservedBoothIds = await _unitOfWork.BoothReservations.AsQueryable()
                .Where(reservation => reservation.ExhibitionID == exhibitionId.Value &&
                                      reservation.BoothID.HasValue &&
                                      reservation.Status != ReservationStatus.Cancelled)
                .Select(reservation => reservation.BoothID!.Value)
                .ToHashSetAsync();
            exhibitionMergeIds = await _unitOfWork.BoothMergeItems.AsQueryable()
                .Where(i => i.ExhibitionID == exhibitionId.Value).ToDictionaryAsync(i => i.BoothID, i => i.MergeID);
            exhibitionMergedBoothIds = exhibitionMergeIds.Keys.ToHashSet();
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<BoothStatus>(status, true, out var boothStatus))
            {
                if (exhibitionReservedBoothIds != null && boothStatus == BoothStatus.Reserved)
                {
                    query = query.Where(b => exhibitionReservedBoothIds.Contains(b.BoothID));
                }
                else if (exhibitionReservedBoothIds != null && boothStatus == BoothStatus.Available)
                {
                    query = query.Where(b =>
                        !exhibitionReservedBoothIds.Contains(b.BoothID) &&
                        b.Status != BoothStatus.Maintenance &&
                        !exhibitionMergedBoothIds!.Contains(b.BoothID));
                }
                else if (exhibitionMergedBoothIds != null && boothStatus == BoothStatus.Merged)
                {
                    query = query.Where(b => exhibitionMergedBoothIds.Contains(b.BoothID));
                }
                else
                {
                    query = query.Where(b => b.Status == boothStatus);
                }
            }
        }

        var totalCount = await query.CountAsync();

        var page = Math.Max(pageNumber ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 10, 1, 200);

        var items = await query
            .OrderBy(b => b.BoothNumber)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var dtos = _mapper.Map<IList<BoothDto>>(items);
        if (exhibitionReservedBoothIds != null)
        {
            foreach (var dto in dtos.Where(dto =>
                         dto.Status is nameof(BoothStatus.Available) or nameof(BoothStatus.Reserved)))
            {
                dto.Status = exhibitionReservedBoothIds.Contains(dto.BoothID)
                    ? BoothStatus.Reserved.ToString()
                    : BoothStatus.Available.ToString();
                dto.IsMerged = exhibitionMergedBoothIds!.Contains(dto.BoothID);
                dto.MergeID = dto.IsMerged ? exhibitionMergeIds![dto.BoothID] : null;
                if (dto.IsMerged) dto.Status = BoothStatus.Merged.ToString();
            }
        }

        return Ok(new PagedResultDto<BoothDto>
        {
            Items = dtos.ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = size
        });
    }

    // GET /api/booths/hall/{hallId}
    [HttpGet("hall/{hallId:int}")]
    public async Task<ActionResult<IList<BoothDto>>> GetByHall(int hallId)
    {
        var result = await _boothService.GetByHallAsync(TenantId, hallId);
        return ToActionResult(result);
    }

    // GET /api/booths/available?hallId=5&exhibitionId=3
    [HttpGet("available")]
    public async Task<ActionResult<IList<BoothSummaryDto>>> GetAvailable(
        [FromQuery] int hallId,
        [FromQuery] int exhibitionId)
    {
        var result = await _boothService.GetAvailableAsync(
            TenantId, hallId, exhibitionId);
        return ToActionResult(result);
    }

    // GET /api/booths/{boothId}
    [HttpGet("{boothId:int}")]
    public async Task<ActionResult<BoothDto>> GetById(int boothId)
    {
        var result = await _boothService.GetByIdAsync(TenantId, boothId);
        return ToActionResult(result);
    }

    // POST /api/booths
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothDto>> Create(
        [FromBody] BoothCreateDto dto)
    {
        var result = await _boothService.CreateAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { boothId = result.Data!.BoothID }, result.Data);
    }

    // PUT /api/booths/{boothId}
    [HttpPut("{boothId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothDto>> Update(
        int boothId,
        [FromBody] BoothUpdateDto dto)
    {
        var result = await _boothService.UpdateAsync(TenantId, boothId, dto);
        return ToActionResult(result);
    }

    // PUT /api/booths/hall/{hallId}/layout
    [HttpPut("hall/{hallId:int}/layout")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothLayoutSaveResultDto>> SaveLayout(
        int hallId,
        [FromBody] BoothLayoutSaveDto dto)
    {
        var result = await _boothService.SaveLayoutAsync(TenantId, UserId, hallId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/booths/{boothId}
    [HttpDelete("{boothId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int boothId)
    {
        var result = await _boothService.DeleteAsync(TenantId, UserId, boothId);
        return ToActionResult(result);
    }

    // POST /api/booths/merge
    [HttpPost("merge")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BoothMergeDto>> Merge(
        [FromBody] BoothMergeCreateDto dto)
    {
        var result = await _boothService.MergeBoothsAsync(TenantId, UserId, dto);
        return ToActionResult(result);
    }

    // DELETE /api/booths/merge/{mergeId}
    [HttpDelete("merge/{mergeId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Unmerge(int mergeId)
    {
        var result = await _boothService.UnmergeBoothsAsync(TenantId, mergeId);
        return ToActionResult(result);
    }
}
