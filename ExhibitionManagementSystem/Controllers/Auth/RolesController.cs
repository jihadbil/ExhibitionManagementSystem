using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Auth;

[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class RolesController : BaseApiController
{
    private readonly IAuthService _authService;

    public RolesController(IAuthService authService)
        => _authService = authService;

    // GET /api/roles
    [HttpGet]
    public async Task<ActionResult<IList<RoleDto>>> GetAll()
    {
        var result = await _authService.GetRolesAsync(TenantId);
        return ToActionResult(result);
    }

    // POST /api/roles/assign
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignRoleDto dto)
    {
        var result = await _authService.AssignRoleAsync(dto);
        return ToActionResult(result);
    }

    // DELETE /api/roles/{userId}/roles/{roleName}
    [HttpDelete("{userId}/roles/{roleName}")]
    public async Task<IActionResult> Remove(string userId, string roleName)
    {
        var result = await _authService.RemoveRoleAsync(userId, roleName);
        return ToActionResult(result);
    }
}
