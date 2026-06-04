using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Auth;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
        => _authService = authService;

    // POST /api/auth/login
    [HttpPost("login")]
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
    [AllowAnonymous]
    public async Task<ActionResult<UserManagementDto>> Register(
        [FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
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
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ResetPasswordRequestDto dto)
    {
        // دائماً 200 OK بغض النظر عن وجود البريد — سلوك أمني مقصود
        await _authService.ForgotPasswordAsync(dto);
        return Ok(new { message = "إذا كان البريد الإلكتروني مسجلاً، ستصلك رسالة لإعادة تعيين كلمة المرور." });
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
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
