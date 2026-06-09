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

        if (hallId.HasValue)
        {
            query = query.Where(b => b.HallID == hallId.Value);
        }

        if (exhibitionId.HasValue)
        {
            var boothIds = await _unitOfWork.BoothReservations.AsQueryable()
                .Where(r => r.ExhibitionID == exhibitionId.Value && r.BoothID.HasValue)
                .Select(r => r.BoothID!.Value)
                .ToListAsync();
            query = query.Where(b => boothIds.Contains(b.BoothID));
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<BoothStatus>(status, true, out var boothStatus))
            {
                query = query.Where(b => b.Status == boothStatus);
            }
        }

        var totalCount = await query.CountAsync();

        var page = pageNumber ?? 1;
        var size = pageSize ?? 10;

        var items = await query
            .OrderBy(b => b.BoothNumber)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var dtos = _mapper.Map<IList<BoothDto>>(items);

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
