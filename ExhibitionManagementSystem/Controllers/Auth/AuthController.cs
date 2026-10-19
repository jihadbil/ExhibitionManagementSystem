using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExhibitionManagementSystem.Controllers.Auth;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
        => _authService = authService;

    // POST /api/auth/login
    [HttpPost("login")]
    [EnableRateLimiting("authentication")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        return Ok(result.Data);
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserManagementDto>> Register(
        [FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(TenantId, dto);
        return ToActionResult(result);
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync(UserId);
        return ToActionResult(result);
    }

    // POST /api/auth/refresh-token
    [HttpPost("refresh-token")]
    [EnableRateLimiting("authentication")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken(
        [FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);
        return ToActionResult(result);
    }

    // POST /api/auth/revoke-token
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken()
    {
        var result = await _authService.RevokeTokenAsync(UserId);
        return ToActionResult(result);
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    [EnableRateLimiting("authentication")]
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ResetPasswordRequestDto dto)
    {
        // نفس الاستجابة للحساب المعروف والمجهول؛ تعطل التهيئة يعيد 503 لكليهما.
        var result = await _authService.ForgotPasswordAsync(dto);
        if (!result.IsSuccess)
            return StatusCode(503, new { result.ErrorMessage, result.ErrorCode });
        return Ok(new { message = "إذا كان البريد الإلكتروني مسجلاً، ستصلك رسالة لإعادة تعيين كلمة المرور." });
    }

    // POST /api/auth/reset-password/confirm
    [HttpPost("reset-password/confirm")]
    [EnableRateLimiting("authentication")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordConfirmDto dto)
    {
        var result = await _authService.ResetPasswordAsync(dto);
        return ToActionResult(result);
    }

    // POST /api/auth/change-password
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto)
    {
        var result = await _authService.ChangePasswordAsync(UserId, dto);
        return ToActionResult(result);
    }
}
