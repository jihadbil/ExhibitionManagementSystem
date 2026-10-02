using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using ExhibitionManagementSystem.Services.Security;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.Security;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        // Login & Registration
        public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email)
                    ?? await _userManager.FindByNameAsync(dto.Email);
            if (user == null || !user.IsActive)
            {
                return InvalidLogin();
            }

            var passwordResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                dto.Password,
                lockoutOnFailure: true);
            if (!passwordResult.Succeeded)
            {
                return InvalidLogin();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantID);
            if (tenant == null)
            {
                return ServiceResult<LoginResponseDto>.Failure("المستأجر الخاص بالمستخدم غير موجود", "TENANT_NOT_FOUND");
            }

            var accessToken = GenerateJwtToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            var refreshExpiryDays = int.Parse(GetRequiredJwtSetting("RefreshTokenExpiryDays"));
            user.LastLogin = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult<LoginResponseDto>.Failure(
                    "تعذر إكمال تسجيل الدخول",
                    "LOGIN_UPDATE_FAILED");
            }

            _dbContext.RefreshSessions.Add(CreateRefreshSession(
                user.Id,
                user.TenantID,
                refreshToken,
                DateTime.UtcNow.AddDays(refreshExpiryDays)));
            await _dbContext.SaveChangesAsync();

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    double.Parse(GetRequiredJwtSetting("AccessTokenExpiryMinutes"))),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                TenantID = user.TenantID,
                TenantName = tenant.CompanyName,
                BaseCurrency = tenant.BaseCurrency,
                Roles = roles.ToList()
            };

            return ServiceResult<LoginResponseDto>.Success(response);
        }

        public async Task<ServiceResult> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            await RevokeAllSessionsAsync(user.Id, "Logout");

            return ServiceResult.Success();
        }

        // JWT & Refresh Token Management
        public async Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var secretKey = GetRequiredJwtSetting("SecretKey");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidIssuer = GetRequiredJwtSetting("Issuer"),
                ValidAudience = GetRequiredJwtSetting("Audience"),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(dto.AccessToken, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return ServiceResult<RefreshTokenResponseDto>.Failure("رمز الوصول غير صالح", "INVALID_ACCESS_TOKEN");
                }
            }
            catch (Exception)
            {
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز الوصول غير صالح", "INVALID_ACCESS_TOKEN");
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantClaim = principal.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(userId) ||
                !int.TryParse(tenantClaim, out var tenantId) ||
                tenantId <= 0)
            {
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز الوصول لا يحتوي على بيانات مستخدم صالحة", "INVALID_ACCESS_TOKEN");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || user.TenantID != tenantId)
            {
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز التحديث غير صالح أو منتهي الصلاحية", "INVALID_REFRESH_TOKEN");
            }

            var tokenHash = RefreshTokenProtector.Hash(dto.RefreshToken);
            var session = await _dbContext.RefreshSessions
                .SingleOrDefaultAsync(candidate =>
                    candidate.UserId == userId &&
                    candidate.TenantID == tenantId &&
                    candidate.TokenHash == tokenHash);

            if (session == null || session.ExpiresAt <= DateTime.UtcNow)
            {
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز التحديث غير صالح أو منتهي الصلاحية", "INVALID_REFRESH_TOKEN");
            }

            if (session.RevokedAt.HasValue)
            {
                await RevokeAllSessionsAsync(userId, "Refresh token reuse detected");
                return ServiceResult<RefreshTokenResponseDto>.Failure(
                    "تم اكتشاف إعادة استخدام رمز تحديث؛ أُغلقت جميع الجلسات",
                    "REFRESH_TOKEN_REUSE_DETECTED");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            var refreshExpiryDays = int.Parse(GetRequiredJwtSetting("RefreshTokenExpiryDays"));
            var replacement = CreateRefreshSession(
                user.Id,
                user.TenantID,
                newRefreshToken,
                DateTime.UtcNow.AddDays(refreshExpiryDays));

            session.LastUsedAt = DateTime.UtcNow;
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "Rotated";
            session.ReplacedBySessionID = replacement.RefreshSessionID;
            _dbContext.RefreshSessions.Add(replacement);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _dbContext.ChangeTracker.Clear();
                await RevokeAllSessionsAsync(userId, "Concurrent refresh detected");
                return ServiceResult<RefreshTokenResponseDto>.Failure(
                    "تم اكتشاف محاولة تحديث متزامنة؛ أُغلقت جميع الجلسات",
                    "REFRESH_TOKEN_REUSE_DETECTED");
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(
                double.Parse(GetRequiredJwtSetting("AccessTokenExpiryMinutes")));

            return ServiceResult<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt
            });
        }

        public async Task<ServiceResult> RevokeTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            await RevokeAllSessionsAsync(user.Id, "Revoked by user");

            return ServiceResult.Success();
        }

        // Password Management
        public async Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "PASSWORD_CHANGE_FAILED");
            }

            await RevokeAllSessionsAsync(user.Id, "Password changed");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ForgotPasswordAsync(ResetPasswordRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return ServiceResult.Success(); // صامت للأمان — لا نكشف وجود/غياب الحساب
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return ServiceResult.Success(token);
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordConfirmDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "PASSWORD_RESET_FAILED");
            }

            await RevokeAllSessionsAsync(user.Id, "Password reset");

            return ServiceResult.Success();
        }

        // Profile Management
        public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<UserProfileDto>.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var profileDto = _mapper.Map<UserProfileDto>(user);
            profileDto.Roles = roles.ToList();

            return ServiceResult<UserProfileDto>.Success(profileDto);
        }

        public async Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<UserProfileDto>.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult<UserProfileDto>.Failure(errors, "PROFILE_UPDATE_FAILED");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var profileDto = _mapper.Map<UserProfileDto>(user);
            profileDto.Roles = roles.ToList();

            return ServiceResult<UserProfileDto>.Success(profileDto);
        }

        // User Administration (Tenant-scoped)
        public async Task<ServiceResult<PagedResultDto<UserManagementDto>>> GetUsersAsync(int tenantId, int page, int pageSize)
        {
            var query = _userManager.Users.Where(u => u.TenantID == tenantId);
            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = new List<UserManagementDto>();
            foreach (var user in users)
            {
                var dto = _mapper.Map<UserManagementDto>(user);
                dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                dtos.Add(dto);
            }

            var result = new PagedResultDto<UserManagementDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<UserManagementDto>>.Success(result);
        }

        public async Task<ServiceResult<UserManagementDto>> GetUserByIdAsync(int tenantId, string userId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantID == tenantId);
            if (user == null)
            {
                return ServiceResult<UserManagementDto>.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<UserManagementDto>(user);
            dto.Roles = roles.ToList();

            return ServiceResult<UserManagementDto>.Success(dto);
        }

        public async Task<ServiceResult<UserManagementDto>> CreateUserAsync(int tenantId, UserManagementCreateDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ServiceResult<UserManagementDto>.Failure("البريد الإلكتروني مستخدم بالفعل", "EMAIL_ALREADY_EXISTS");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                TenantID = tenantId,
                IsActive = dto.IsActive
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return ServiceResult<UserManagementDto>.Failure($"فشل إنشاء الحساب: {errors}", "USER_CREATION_FAILED");
            }

            if (dto.Roles != null && dto.Roles.Count > 0)
            {
                foreach (var role in dto.Roles)
                {
                    if (!SystemRoles.TenantAssignable.Contains(role))
                    {
                        await _userManager.DeleteAsync(user);
                        return ServiceResult<UserManagementDto>.Failure(
                            "لا يمكن منح دور إدارة المنصة من هذا المسار",
                            "UNAUTHORIZED");
                    }

                    var tenantRole = await FindTenantRoleAsync(tenantId, role);
                    if (tenantRole?.Name == null)
                    {
                        await _userManager.DeleteAsync(user);
                        return ServiceResult<UserManagementDto>.Failure(
                            "الدور المحدد غير موجود لهذا المستأجر",
                            "ROLE_NOT_FOUND");
                    }

                    await _userManager.AddToRoleAsync(user, tenantRole.Name);
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserManagementDto>(user);
            userDto.Roles = roles.ToList();

            return ServiceResult<UserManagementDto>.Success(userDto);
        }

        public async Task<ServiceResult> UpdateUserStatusAsync(
            int tenantId,
            string actorUserId,
            string userId,
            bool isActive)
        {
            if (actorUserId == userId)
            {
                return ServiceResult.Failure(
                    "لا يمكن للمستخدم تغيير حالة حسابه من هذا المسار",
                    "SELF_MANAGEMENT_FORBIDDEN");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantID == tenantId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            if (!isActive && await IsLastTenantAdminAsync(tenantId, user))
            {
                return ServiceResult.Failure(
                    "لا يمكن تعطيل آخر مدير للمستأجر",
                    "LAST_TENANT_ADMIN");
            }

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "STATUS_UPDATE_FAILED");
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteUserAsync(
            int tenantId,
            string actorUserId,
            string userId)
        {
            if (actorUserId == userId)
            {
                return ServiceResult.Failure(
                    "لا يمكن للمستخدم حذف حسابه من هذا المسار",
                    "SELF_MANAGEMENT_FORBIDDEN");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantID == tenantId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            if (await IsLastTenantAdminAsync(tenantId, user))
            {
                return ServiceResult.Failure(
                    "لا يمكن حذف آخر مدير للمستأجر",
                    "LAST_TENANT_ADMIN");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "USER_DELETION_FAILED");
            }

            return ServiceResult.Success();
        }

        // Role Management
        public async Task<ServiceResult<IList<RoleDto>>> GetRolesAsync(int tenantId)
        {
            _ = tenantId;
            var assignableRoleNames = SystemRoles.TenantAssignable.ToArray();
            var roles = await _roleManager.Roles
                .Where(role => role.Name != null &&
                    assignableRoleNames.Contains(role.Name))
                .ToListAsync();

            var dtos = _mapper.Map<IList<RoleDto>>(roles);
            return ServiceResult<IList<RoleDto>>.Success(dtos);
        }

        public async Task<ServiceResult> AssignRoleAsync(int tenantId, AssignRoleDto dto)
        {
            if (!SystemRoles.TenantAssignable.Contains(dto.RoleName))
            {
                return ServiceResult.Failure(
                    "لا يمكن منح دور إدارة المنصة من هذا المسار",
                    "UNAUTHORIZED");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.TenantID == tenantId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var tenantRole = await FindTenantRoleAsync(tenantId, dto.RoleName);
            if (tenantRole?.Name == null)
            {
                return ServiceResult.Failure(
                    "الدور المحدد غير موجود لهذا المستأجر",
                    "ROLE_NOT_FOUND");
            }

            var isInRole = await _userManager.IsInRoleAsync(user, tenantRole.Name);
            if (isInRole)
            {
                return ServiceResult.Failure("المستخدم يمتلك هذا الدور بالفعل", "ROLE_ALREADY_ASSIGNED");
            }

            var result = await _userManager.AddToRoleAsync(user, tenantRole.Name);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "ROLE_ASSIGNMENT_FAILED");
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RemoveRoleAsync(int tenantId, string userId, string roleName)
        {
            if (!SystemRoles.TenantAssignable.Contains(roleName))
            {
                return ServiceResult.Failure(
                    "لا يمكن تعديل دور إدارة المنصة من هذا المسار",
                    "UNAUTHORIZED");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantID == tenantId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var tenantRole = await FindTenantRoleAsync(tenantId, roleName);
            if (tenantRole?.Name == null)
            {
                return ServiceResult.Failure(
                    "الدور المحدد غير موجود لهذا المستأجر",
                    "ROLE_NOT_FOUND");
            }

            if ((string.Equals(tenantRole.Name, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(tenantRole.Name, SystemRoles.TenantAdmin, StringComparison.OrdinalIgnoreCase)) &&
                await IsLastTenantAdminAsync(tenantId, user))
            {
                return ServiceResult.Failure(
                    "لا يمكن إزالة دور آخر مدير للمستأجر",
                    "LAST_TENANT_ADMIN");
            }

            var isInRole = await _userManager.IsInRoleAsync(user, tenantRole.Name);
            if (!isInRole)
            {
                return ServiceResult.Failure("المستخدم لا يمتلك هذا الدور", "ROLE_NOT_ASSIGNED");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, tenantRole.Name);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "ROLE_REMOVAL_FAILED");
            }

            return ServiceResult.Success();
        }

        // Helpers
        private Task<ApplicationRole?> FindTenantRoleAsync(int tenantId, string roleName)
        {
            _ = tenantId;
            var normalizedRoleName = _roleManager.NormalizeKey(roleName);
            return _roleManager.Roles.FirstOrDefaultAsync(
                role => role.NormalizedName == normalizedRoleName);
        }

        private async Task<bool> IsLastTenantAdminAsync(
            int tenantId,
            ApplicationUser user)
        {
            var isTenantAdmin = await _userManager.IsInRoleAsync(user, SystemRoles.Admin) ||
                                await _userManager.IsInRoleAsync(user, SystemRoles.TenantAdmin);
            if (!isTenantAdmin)
            {
                return false;
            }

            var legacyAdmins = await _userManager.GetUsersInRoleAsync(SystemRoles.Admin);
            var tenantAdmins = await _userManager.GetUsersInRoleAsync(SystemRoles.TenantAdmin);
            return legacyAdmins
                .Concat(tenantAdmins)
                .Where(admin => admin.TenantID == tenantId && admin.IsActive)
                .Select(admin => admin.Id)
                .Distinct(StringComparer.Ordinal)
                .Count() <= 1;
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var secretKey = GetRequiredJwtSetting("SecretKey");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("TenantId", user.TenantID.ToString()),
                new Claim("FullName", user.FullName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(
                    double.Parse(GetRequiredJwtSetting("AccessTokenExpiryMinutes"))),
                Issuer = GetRequiredJwtSetting("Issuer"),
                Audience = GetRequiredJwtSetting("Audience"),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private ServiceResult<LoginResponseDto> InvalidLogin() =>
            ServiceResult<LoginResponseDto>.Failure(
                "بيانات تسجيل الدخول غير صحيحة",
                "INVALID_CREDENTIALS");

        private string GetRequiredJwtSetting(string name)
        {
            var value = _configuration[$"JwtSettings:{name}"];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Required JWT setting 'JwtSettings:{name}' is missing.");
            }

            return value;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private RefreshSession CreateRefreshSession(
            string userId,
            int tenantId,
            string rawToken,
            DateTime expiresAt)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var userAgent = request?.Headers.UserAgent.ToString();

            return new RefreshSession
            {
                UserId = userId,
                TenantID = tenantId,
                TokenHash = RefreshTokenProtector.Hash(rawToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                CreatedByIp = Truncate(
                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    50),
                DeviceDescription = Truncate(userAgent, 256)
            };
        }

        private async Task RevokeAllSessionsAsync(string userId, string reason)
        {
            var now = DateTime.UtcNow;
            var sessions = await _dbContext.RefreshSessions
                .Where(session =>
                    session.UserId == userId &&
                    session.RevokedAt == null &&
                    session.ExpiresAt > now)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.RevokedAt = now;
                session.LastUsedAt = now;
                session.RevocationReason = Truncate(reason, 100);
            }

            if (sessions.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        private static string? Truncate(string? value, int maxLength) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : value.Length <= maxLength
                    ? value
                    : value[..maxLength];
    }
}
