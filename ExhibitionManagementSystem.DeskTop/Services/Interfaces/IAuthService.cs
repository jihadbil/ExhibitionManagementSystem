using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IAuthService
    {
        // Login & Registration
        Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
        Task<ServiceResult<UserManagementDto>> RegisterAsync(RegisterRequestDto dto);
        Task<ServiceResult> LogoutAsync(string userId);

        // JWT & Refresh Token Management
        Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
        Task<ServiceResult> RevokeTokenAsync(string userId);

        // Password Management
        Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ServiceResult> ForgotPasswordAsync(ResetPasswordRequestDto dto);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordConfirmDto dto);

        // Profile Management
        Task<ServiceResult<UserProfileDto>> GetProfileAsync(string userId);
        Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);

        // User Administration (Tenant-scoped)
        Task<ServiceResult<PagedResultDto<UserManagementDto>>> GetUsersAsync(int tenantId, int page, int pageSize);
        Task<ServiceResult<UserManagementDto>> GetUserByIdAsync(string userId);
        Task<ServiceResult<UserManagementDto>> CreateUserAsync(int tenantId, UserManagementCreateDto dto);
        Task<ServiceResult> UpdateUserStatusAsync(string userId, bool isActive);
        Task<ServiceResult> DeleteUserAsync(string userId);

        // Role Management
        Task<ServiceResult<IList<RoleDto>>> GetRolesAsync(int tenantId);
        Task<ServiceResult> AssignRoleAsync(AssignRoleDto dto);
        Task<ServiceResult> RemoveRoleAsync(string userId, string roleName);
    }
}
