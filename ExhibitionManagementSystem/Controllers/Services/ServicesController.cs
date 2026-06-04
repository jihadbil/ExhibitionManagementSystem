using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Services;

[Route("api/[controller]")]
public class ServicesController : BaseApiController
{
    private readonly IServiceManagementService _serviceService;

    public ServicesController(IServiceManagementService serviceService)
        => _serviceService = serviceService;

    // GET /api/services
    [HttpGet]
    public async Task<ActionResult<IList<ServiceDto>>> GetAll()
    {
        var result = await _serviceService.GetByTenantAsync(TenantId);
        return ToActionResult(result);
    }

    // GET /api/services/{serviceId}
    [HttpGet("{serviceId:int}")]
    public async Task<ActionResult<ServiceDto>> GetById(int serviceId)
    {
        var result = await _serviceService.GetByIdAsync(TenantId, serviceId);
        return ToActionResult(result);
    }

    // POST /api/services
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceDto>> Create(
        [FromBody] ServiceCreateDto dto)
    {
        var result = await _serviceService.CreateAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { serviceId = result.Data!.ServiceID }, result.Data);
    }

    // PUT /api/services/{serviceId}
    [HttpPut("{serviceId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ServiceDto>> Update(
        int serviceId,
        [FromBody] ServiceCreateDto dto)
    {
        var result = await _serviceService.UpdateAsync(TenantId, serviceId, dto);
        return ToActionResult(result);
    }

    // PATCH /api/services/{serviceId}/deactivate
    [HttpPatch("{serviceId:int}/deactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deactivate(int serviceId)
    {
        var result = await _serviceService.DeactivateAsync(TenantId, serviceId);
        return ToActionResult(result);
    }
}
