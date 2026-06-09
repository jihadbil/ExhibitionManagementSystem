using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExhibitionManagementSystem.Controllers.Auth;

[Route("api/[controller]")]
public class ProfileController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProfileController(IAuthService authService, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _authService = authService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // GET /api/profile
    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var result = await _authService.GetProfileAsync(UserId);
        return ToActionResult(result);
    }

    // PUT /api/profile
    [HttpPut]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileDto dto)
    {
        var result = await _authService.UpdateProfileAsync(UserId, dto);
        return ToActionResult(result);
    }

    // GET /api/profile/audit-logs
    [HttpGet("audit-logs")]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetProfileAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var tenantId = TenantId;
        var userId = UserId;

        var query = _unitOfWork.AuditLogs.AsQueryable()
            .Include(a => a.User)
            .Where(a => a.TenantID == tenantId && a.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.ActionAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<IList<AuditLogDto>>(items);

        var result = new PagedResultDto<AuditLogDto>
        {
            Items = dtos.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(result);
    }
}
