using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Auth;

public class AuthApiClient : ApiClientBase, IAuthService
{
    public AuthApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        return PostAsync<LoginResponseDto>("api/auth/login", dto);
    }

    public Task<ServiceResult<UserManagementDto>> RegisterAsync(RegisterRequestDto dto)
    {
        return PostAsync<UserManagementDto>("api/auth/register", dto);
    }

    public Task<ServiceResult> LogoutAsync(string userId)
    {
        return PostVoidAsync("api/auth/logout", new { });
    }

    public Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        return PostAsync<RefreshTokenResponseDto>("api/auth/refresh-token", dto);
    }

    public Task<ServiceResult> RevokeTokenAsync(string userId)
    {
        return PostVoidAsync("api/auth/revoke-token", new { });
    }

    public Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        return PostVoidAsync("api/auth/change-password", dto);
    }

    public Task<ServiceResult> ForgotPasswordAsync(ResetPasswordRequestDto dto)
    {
        return PostVoidAsync("api/auth/forgot-password", dto);
    }

    public Task<ServiceResult> ResetPasswordAsync(ResetPasswordConfirmDto dto)
    {
        return PostVoidAsync("api/auth/reset-password/confirm", dto);
    }

    public Task<ServiceResult<UserProfileDto>> GetProfileAsync(string userId)
    {
        return GetAsync<UserProfileDto>("api/profile");
    }

    public Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        return PutAsync<UserProfileDto>("api/profile", dto);
    }

    public Task<ServiceResult<PagedResultDto<UserManagementDto>>> GetUsersAsync(int tenantId, int page, int pageSize)
    {
        return GetAsync<PagedResultDto<UserManagementDto>>($"api/users?page={page}&pageSize={pageSize}");
    }

    public Task<ServiceResult<UserManagementDto>> GetUserByIdAsync(string userId)
    {
        return GetAsync<UserManagementDto>($"api/users/{userId}");
    }

    public Task<ServiceResult<UserManagementDto>> CreateUserAsync(int tenantId, UserManagementCreateDto dto)
    {
        return PostAsync<UserManagementDto>("api/users", dto);
    }

    public Task<ServiceResult> UpdateUserStatusAsync(string userId, bool isActive)
    {
        return PatchVoidAsync($"api/users/{userId}/status", isActive);
    }

    public Task<ServiceResult> DeleteUserAsync(string userId)
    {
        return DeleteAsync($"api/users/{userId}");
    }

    public Task<ServiceResult<IList<RoleDto>>> GetRolesAsync(int tenantId)
    {
        return GetAsync<IList<RoleDto>>("api/roles");
    }

    public Task<ServiceResult> AssignRoleAsync(AssignRoleDto dto)
    {
        return PostVoidAsync("api/roles/assign", dto);
    }

    public Task<ServiceResult> RemoveRoleAsync(string userId, string roleName)
    {
        return DeleteAsync($"api/roles/{userId}/roles/{roleName}");
    }
}
