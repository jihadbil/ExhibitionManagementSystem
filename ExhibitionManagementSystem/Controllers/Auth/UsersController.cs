using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Auth;

[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : BaseApiController
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
        => _authService = authService;

    // GET /api/users?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<UserManagementDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _authService.GetUsersAsync(TenantId, page, pageSize);
        return ToActionResult(result);
    }

    // GET /api/users/{userId}
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserManagementDto>> GetById(string userId)
    {
        var result = await _authService.GetUserByIdAsync(userId);
        return ToActionResult(result);
    }

    // POST /api/users
    [HttpPost]
    public async Task<ActionResult<UserManagementDto>> Create(
        [FromBody] UserManagementCreateDto dto)
    {
        var result = await _authService.CreateUserAsync(TenantId, dto);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById),
            new { userId = result.Data!.UserId }, result.Data);
    }

    // PATCH /api/users/{userId}/status
    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> UpdateStatus(
        string userId,
        [FromBody] bool isActive)
    {
        var result = await _authService.UpdateUserStatusAsync(userId, isActive);
        return ToActionResult(result);
    }

    // DELETE /api/users/{userId}
    [HttpDelete("{userId}")]
    public async Task<IActionResult> Delete(string userId)
    {
        var result = await _authService.DeleteUserAsync(userId);
        return ToActionResult(result);
    }
}
