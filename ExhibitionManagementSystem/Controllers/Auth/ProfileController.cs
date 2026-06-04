using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Auth;

[Route("api/[controller]")]
public class ProfileController : BaseApiController
{
    private readonly IAuthService _authService;

    public ProfileController(IAuthService authService)
        => _authService = authService;

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
}
