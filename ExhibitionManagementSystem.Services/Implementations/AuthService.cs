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

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // Login & Registration
        public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return ServiceResult<LoginResponseDto>.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            if (!user.IsActive)
            {
                return ServiceResult<LoginResponseDto>.Failure("الحساب غير نشط", "USER_INACTIVE");
            }

            var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
            if (!passwordResult.Succeeded)
            {
                return ServiceResult<LoginResponseDto>.Failure("كلمة المرور غير صحيحة", "INVALID_CREDENTIALS");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(user.TenantID);
            if (tenant == null)
            {
                return ServiceResult<LoginResponseDto>.Failure("المستأجر الخاص بالمستخدم غير موجود", "TENANT_NOT_FOUND");
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

        public async Task<ServiceResult<UserManagementDto>> RegisterAsync(RegisterRequestDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return ServiceResult<UserManagementDto>.Failure("البريد الإلكتروني مستخدم بالفعل", "EMAIL_ALREADY_EXISTS");
            }

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(dto.TenantID);
            if (tenant == null)
            {
                return ServiceResult<UserManagementDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                TenantID = dto.TenantID,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return ServiceResult<UserManagementDto>.Failure($"فشل إنشاء الحساب: {errors}", "REGISTRATION_FAILED");
            }

            if (!string.IsNullOrEmpty(dto.InitialRole))
            {
                var roleExists = await _roleManager.RoleExistsAsync(dto.InitialRole);
                if (!roleExists)
                {
                    var newRole = new ApplicationRole { Name = dto.InitialRole, TenantID = dto.TenantID };
                    await _roleManager.CreateAsync(newRole);
                }
                await _userManager.AddToRoleAsync(user, dto.InitialRole);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserManagementDto>(user);
            userDto.Roles = roles.ToList();

            return ServiceResult<UserManagementDto>.Success(userDto);
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
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? "DefaultSecretKeyHere";
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
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
            if (user == null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                return ServiceResult<RefreshTokenResponseDto>.Failure("رمز التحديث غير صالح أو منتهي الصلاحية", "INVALID_REFRESH_TOKEN");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            var refreshExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshExpiryDays);
            await _userManager.UpdateAsync(user);

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

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ForgotPasswordAsync(ResetPasswordRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // للسلامة الأمنية لا نخبر المخترقين بعدم وجود الحساب، ولكن هنا للـ API نريد دعمه بشكل كامل
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
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

        public async Task<ServiceResult<UserManagementDto>> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
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
                    var roleExists = await _roleManager.RoleExistsAsync(role);
                    if (!roleExists)
                    {
                        var newRole = new ApplicationRole { Name = role, TenantID = tenantId };
                        await _roleManager.CreateAsync(newRole);
                    }
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserManagementDto>(user);
            userDto.Roles = roles.ToList();

            return ServiceResult<UserManagementDto>.Success(userDto);
        }

        public async Task<ServiceResult> UpdateUserStatusAsync(string userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
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

        public async Task<ServiceResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
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
            var roles = await _roleManager.Roles
                .Where(r => r.TenantID == tenantId)
                .ToListAsync();

            var dtos = _mapper.Map<IList<RoleDto>>(roles);
            return ServiceResult<IList<RoleDto>>.Success(dtos);
        }

        public async Task<ServiceResult> AssignRoleAsync(AssignRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

            var roleExists = await _roleManager.RoleExistsAsync(dto.RoleName);
            if (!roleExists)
            {
                var newRole = new ApplicationRole { Name = dto.RoleName, TenantID = user.TenantID };
                var createRoleResult = await _roleManager.CreateAsync(newRole);
                if (!createRoleResult.Succeeded)
                {
                    return ServiceResult.Failure("فشل إنشاء الدور المحدد", "ROLE_CREATION_FAILED");
                }
            }

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

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RemoveRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("المستخدم غير موجود", "USER_NOT_FOUND");
            }

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

            return ServiceResult.Success();
        }

        // Helpers
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? "DefaultSecretKeyHere";
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
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "60")),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
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
