using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ExhibitionManagementSystem.Services.Messaging;

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
        private readonly IPasswordResetDelivery _passwordResetDelivery;
        private readonly IUserRoleReader _userRoleReader;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IMapper mapper,
            IUnitOfWork unitOfWork, IPasswordResetDelivery passwordResetDelivery, IUserRoleReader userRoleReader)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _passwordResetDelivery = passwordResetDelivery;
            _userRoleReader = userRoleReader;
        }

        // Login & Registration
        public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return ServiceResult<LoginResponseDto>.Failure("بيانات الدخول غير صحيحة", "INVALID_CREDENTIALS");
            }

            if (!user.IsActive)
            {
                return ServiceResult<LoginResponseDto>.Failure("بيانات الدخول غير صحيحة", "INVALID_CREDENTIALS");
            }

            var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!passwordResult.Succeeded)
            {
                return ServiceResult<LoginResponseDto>.Failure("بيانات الدخول غير صحيحة", "INVALID_CREDENTIALS");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantID);
            if (tenant == null || !tenant.IsActive)
            {
                return ServiceResult<LoginResponseDto>.Failure("بيانات الدخول غير صحيحة", "INVALID_CREDENTIALS");
            }

            var accessToken = GenerateJwtToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            var refreshExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshExpiryDays);
            user.LastLogin = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "60")),
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

        public Task<ServiceResult<UserManagementDto>> RegisterAsync(int tenantId, RegisterRequestDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return Task.FromResult(ServiceResult<UserManagementDto>.Failure("كلمتا المرور غير متطابقتين", "INVALID_PASSWORD_CONFIRMATION"));

            // Closed registration uses the authenticated tenant. Platform roles
            // are provisioned only through the explicit user-administration path.
            return CreateUserAsync(tenantId, new UserManagementCreateDto
            {
                FullName = dto.FullName, Email = dto.Email, Password = dto.Password,
                IsActive = true, Roles = [dto.InitialRole ?? "Staff"]
            });
        }

        public async Task<ServiceResult> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateAsync(user);

            return ServiceResult.Success();
        }

        // JWT & Refresh Token Management
        public async Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var secretKey = GetSigningKey();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"] ?? "ExhibitionManagementSystem",
                ValidAudience = _configuration["JwtSettings:Audience"] ?? "ExhibitionManagementSystemClients",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز الوصول لا يحتوي على معرف مستخدم", "INVALID_ACCESS_TOKEN");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || string.IsNullOrEmpty(user.RefreshToken)
                || user.RefreshToken != dto.RefreshToken || !user.RefreshTokenExpiry.HasValue
                || user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز التحديث غير صالح أو منتهي الصلاحية", "INVALID_REFRESH_TOKEN");
            }

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantID);
            if (tenant == null || !tenant.IsActive)
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز التحديث غير صالح", "INVALID_REFRESH_TOKEN");

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            var refreshExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshExpiryDays);
            // Identity's concurrency stamp makes refresh rotation single-use.
            var rotation = await _userManager.UpdateAsync(user);
            if (!rotation.Succeeded)
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز التحديث غير صالح", "INVALID_REFRESH_TOKEN");

            var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "60"));

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

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateAsync(user);

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

            await InvalidateSessionsAsync(user);
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ForgotPasswordAsync(ResetPasswordRequestDto dto)
        {
            if (!_passwordResetDelivery.IsAvailable)
                return ServiceResult.Failure("خدمة إعادة التعيين غير مهيأة", "PASSWORD_RESET_UNAVAILABLE");
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive || !await _unitOfWork.Tenants.AsQueryable().AnyAsync(t => t.TenantID == user.TenantID && t.IsActive, cancellationToken: _unitOfWork.RequestCancellation))
            {
                return ServiceResult.Success(); // صامت للأمان — لا نكشف وجود/غياب الحساب
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _passwordResetDelivery.QueueAsync(user, token, _unitOfWork.RequestCancellation);
            return ServiceResult.Success();
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

            await InvalidateSessionsAsync(user);
            return ServiceResult.Success();
        }

        // Profile Management
        public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: _unitOfWork.RequestCancellation);

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
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: _unitOfWork.RequestCancellation);

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
            var totalCount = await query.CountAsync(cancellationToken: _unitOfWork.RequestCancellation);
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var roleNames = await _userRoleReader.ReadAsync(users.Select(u => u.Id), _unitOfWork.RequestCancellation);
            var dtos = new List<UserManagementDto>();
            foreach (var user in users)
            {
                var dto = _mapper.Map<UserManagementDto>(user);
                dto.Roles = roleNames.GetValueOrDefault(user.Id) ?? [];
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
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantID != tenantId)
            {
                return ServiceResult<UserManagementDto>.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<UserManagementDto>(user);
            dto.Roles = roles.ToList();

            return ServiceResult<UserManagementDto>.Success(dto);
        }

        public async Task<ServiceResult<UserManagementDto>> CreateUserAsync(int tenantId, UserManagementCreateDto dto, bool canManagePlatformRoles = false)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null || !tenant.IsActive)
                return ServiceResult<UserManagementDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            var requestedRoles = (dto.Roles ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var role in requestedRoles)
            {
                if (!CanAssignRole(role, canManagePlatformRoles))
                    return ServiceResult<UserManagementDto>.Failure("لا يمكن إسناد الدور المطلوب", "FORBIDDEN");
                if (!await _roleManager.RoleExistsAsync(role))
                    return ServiceResult<UserManagementDto>.Failure("الدور غير موجود", "ROLE_NOT_FOUND");
            }

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

            if (requestedRoles.Count > 0)
            {
                var assignment = await _userManager.AddToRolesAsync(user, requestedRoles);
                if (!assignment.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return ServiceResult<UserManagementDto>.Failure("فشل إسناد الأدوار", "ROLE_ASSIGNMENT_FAILED");
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserManagementDto>(user);
            userDto.Roles = roles.ToList();

            return ServiceResult<UserManagementDto>.Success(userDto);
        }

        public async Task<ServiceResult> UpdateUserStatusAsync(int tenantId, string userId, bool isActive, bool canManagePlatformRoles = false)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantID != tenantId)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            if (!canManagePlatformRoles && await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                return ServiceResult.Failure("لا يمكن تعديل حساب إدارة المنصة", "FORBIDDEN");
            user.IsActive = isActive;
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.SecurityStamp = Guid.NewGuid().ToString();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "STATUS_UPDATE_FAILED");
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteUserAsync(int tenantId, string userId, bool canManagePlatformRoles = false)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantID != tenantId)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            if (!canManagePlatformRoles && await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                return ServiceResult.Failure("لا يمكن حذف حساب إدارة المنصة", "FORBIDDEN");
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
            var roles = await _roleManager.Roles
                .Where(r => r.Name == "Admin" || r.Name == "Staff" || r.Name == "Exhibitor")
                .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

            var dtos = _mapper.Map<IList<RoleDto>>(roles);
            return ServiceResult<IList<RoleDto>>.Success(dtos);
        }

        public async Task<ServiceResult> AssignRoleAsync(int tenantId, AssignRoleDto dto, bool canManagePlatformRoles = false)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null || user.TenantID != tenantId)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            if (!CanAssignRole(dto.RoleName, canManagePlatformRoles)
                || (!canManagePlatformRoles && await _userManager.IsInRoleAsync(user, "SuperAdmin")))
                return ServiceResult.Failure("لا يمكن إسناد الدور المطلوب", "FORBIDDEN");
            if (!await _roleManager.RoleExistsAsync(dto.RoleName))
                return ServiceResult.Failure("الدور غير موجود", "ROLE_NOT_FOUND");

            var isInRole = await _userManager.IsInRoleAsync(user, dto.RoleName);
            if (isInRole)
            {
                return ServiceResult.Failure("المستخدم يمتلك هذا الدور بالفعل", "ROLE_ALREADY_ASSIGNED");
            }

            var result = await _userManager.AddToRoleAsync(user, dto.RoleName);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "ROLE_ASSIGNMENT_FAILED");
            }

            await InvalidateSessionsAsync(user);
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RemoveRoleAsync(int tenantId, string userId, string roleName, bool canManagePlatformRoles = false)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.TenantID != tenantId)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            if (!CanAssignRole(roleName, canManagePlatformRoles)
                || (!canManagePlatformRoles && await _userManager.IsInRoleAsync(user, "SuperAdmin")))
                return ServiceResult.Failure("لا يمكن تعديل الدور المطلوب", "FORBIDDEN");
            var isInRole = await _userManager.IsInRoleAsync(user, roleName);
            if (!isInRole)
            {
                return ServiceResult.Failure("المستخدم لا يمتلك هذا الدور", "ROLE_NOT_ASSIGNED");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors, "ROLE_REMOVAL_FAILED");
            }

            await InvalidateSessionsAsync(user);
            return ServiceResult.Success();
        }

        // Roles are global Identity roles; resource access remains tenant-scoped.
        private static bool CanAssignRole(string role, bool platformAdmin) =>
            role is "Admin" or "Staff" or "Exhibitor" || (platformAdmin && role == "SuperAdmin");

        private async Task InvalidateSessionsAsync(ApplicationUser user)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateSecurityStampAsync(user);
        }

        private string GetSigningKey()
        {
            var key = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
                throw new InvalidOperationException("JwtSettings:SecretKey must contain at least 32 bytes.");
            return key;
        }

        // Helpers
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var secretKey = GetSigningKey();
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("TenantId", user.TenantID.ToString()),
                new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
                new Claim("FullName", user.FullName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "60")),
                Issuer = _configuration["JwtSettings:Issuer"] ?? "ExhibitionManagementSystem",
                Audience = _configuration["JwtSettings:Audience"] ?? "ExhibitionManagementSystemClients",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
