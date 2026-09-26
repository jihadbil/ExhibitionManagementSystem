using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Security;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.Services.Options;
using ExhibitionManagementSystem.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class TenantService : ITenantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ITenantInvitationSender _invitationSender;
        private readonly TenantInvitationOptions _invitationOptions;
        private readonly ILogger<TenantService> _logger;

        public TenantService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ITenantInvitationSender invitationSender,
            IOptions<TenantInvitationOptions> invitationOptions,
            ILogger<TenantService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _invitationSender = invitationSender;
            _invitationOptions = invitationOptions.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResultDto<TenantDto>>> GetAllAsync(int page, int pageSize)
        {
            var (items, totalCount) = await _unitOfWork.Tenants.GetPagedAsync(page, pageSize);
            
            var dtos = _mapper.Map<List<TenantDto>>(items);
            
            var result = new PagedResultDto<TenantDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<TenantDto>>.Success(result);
        }

        public async Task<ServiceResult<TenantDto>> GetByIdAsync(int tenantId)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            var dto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(dto);
        }

        public async Task<ServiceResult<TenantDto>> CreateAsync(
            TenantCreateDto dto,
            string actorUserId)
        {
            var normalizedSubdomain = dto.Subdomain.Trim().ToLowerInvariant();
            var normalizedEmail = _userManager.NormalizeEmail(dto.AdminEmail.Trim());

            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return ServiceResult<TenantDto>.Failure(
                    "تعذر تحديد منفذ العملية.",
                    "UNAUTHORIZED");
            }

            if (await _dbContext.Tenants.AnyAsync(tenant =>
                    tenant.Subdomain == normalizedSubdomain))
            {
                return ServiceResult<TenantDto>.Failure("النطاق الفرعي مستخدم بالفعل", "SUBDOMAIN_ALREADY_EXISTS");
            }

            var pendingInvitations = await _dbContext.TenantInvitations
                .Where(invitation =>
                    invitation.NormalizedEmail == normalizedEmail &&
                    invitation.AcceptedAt == null &&
                    invitation.RevokedAt == null)
                .ToListAsync();
            if (await _userManager.FindByEmailAsync(dto.AdminEmail.Trim()) != null ||
                pendingInvitations.Any(invitation => invitation.ExpiresAt > DateTime.UtcNow))
            {
                return ServiceResult<TenantDto>.Failure(
                    "البريد الإلكتروني مستخدم أو لديه دعوة نشطة بالفعل.",
                    "EMAIL_ALREADY_EXISTS");
            }

            var now = DateTime.UtcNow;
            var trialEndsAt = dto.TrialEndsAt?.ToUniversalTime() ?? now.AddDays(14);
            var tenant = _mapper.Map<Tenant>(dto);
            tenant.Subdomain = normalizedSubdomain;
            tenant.BaseCurrency = dto.BaseCurrency.Trim().ToUpperInvariant();
            tenant.TrialEndsAt = trialEndsAt;
            tenant.IsActive = true;

            var subscription = new TenantSubscription
            {
                Tenant = tenant,
                Plan = dto.Plan.Trim(),
                StartDate = now.Date,
                EndDate = trialEndsAt.Date,
                MonthlyFee = 0,
                CurrencyCode = tenant.BaseCurrency,
                Status = SubscriptionStatus.Trial
            };

            var rawToken = TenantInvitationTokenService.GenerateToken();
            var invitation = new TenantInvitation
            {
                Tenant = tenant,
                Email = dto.AdminEmail.Trim(),
                NormalizedEmail = normalizedEmail,
                FullName = dto.AdminFullName.Trim(),
                TokenHash = TenantInvitationTokenService.Hash(rawToken),
                ExpiresAt = now.AddHours(GetInvitationExpiryHours()),
                CreatedByUserId = actorUserId
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var expiredInvitation in pendingInvitations)
                {
                    expiredInvitation.RevokedAt = now;
                    expiredInvitation.UpdatedAt = now;
                }
                if (pendingInvitations.Count > 0)
                {
                    await _dbContext.SaveChangesAsync();
                }

                await EnsureSystemRolesAsync();
                _dbContext.Tenants.Add(tenant);
                _dbContext.TenantSubscriptions.Add(subscription);
                _dbContext.TenantInvitations.Add(invitation);
                await _dbContext.SaveChangesAsync();

                _dbContext.AuditLogs.Add(CreateAuditLog(
                    tenant.TenantID,
                    actorUserId,
                    "TenantProvisioning",
                    tenant.TenantID.ToString(),
                    new
                    {
                        tenant.CompanyName,
                        tenant.Subdomain,
                        subscription.Plan,
                        AdminEmail = invitation.Email,
                        InvitationExpiresAt = invitation.ExpiresAt
                    }));
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return ServiceResult<TenantDto>.Failure(
                    "تعذر إنشاء المستأجر بسبب تعارض في البيانات.",
                    "TENANT_PROVISIONING_CONFLICT");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("ROLE_SETUP_FAILED:", StringComparison.Ordinal))
            {
                await transaction.RollbackAsync();
                return ServiceResult<TenantDto>.Failure(
                    "تعذر تجهيز أدوار النظام.",
                    "ROLE_SETUP_FAILED");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Tenant provisioning failed for subdomain {Subdomain}.", normalizedSubdomain);
                return ServiceResult<TenantDto>.Failure(
                    "تعذر إكمال تهيئة المستأجر.",
                    "TENANT_PROVISIONING_FAILED");
            }

            try
            {
                await _invitationSender.SendAsync(
                    invitation.Email,
                    invitation.FullName,
                    tenant.CompanyName,
                    rawToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant invitation delivery failed for tenant {TenantId}.", tenant.TenantID);
                return ServiceResult<TenantDto>.Failure(
                    "تم إنشاء المستأجر، لكن تعذر إرسال الدعوة. استخدم إعادة إرسال الدعوة.",
                    "TENANT_CREATED_INVITATION_DELIVERY_FAILED");
            }

            var createdTenant = await _unitOfWork.Tenants.GetByIdAsync(tenant.TenantID);
            var resultDto = _mapper.Map<TenantDto>(createdTenant ?? tenant);

            return ServiceResult<TenantDto>.Success(resultDto);
        }

        public async Task<ServiceResult> AcceptInvitationAsync(TenantInvitationAcceptDto dto)
        {
            var normalizedEmail = _userManager.NormalizeEmail(dto.Email.Trim());
            var tokenHash = TenantInvitationTokenService.Hash(dto.Token);
            var invitation = await _dbContext.TenantInvitations
                .Include(candidate => candidate.Tenant)
                .FirstOrDefaultAsync(candidate =>
                    candidate.NormalizedEmail == normalizedEmail &&
                    candidate.TokenHash == tokenHash);

            if (invitation == null || !TenantInvitationTokenService.Verify(dto.Token, invitation.TokenHash))
            {
                return ServiceResult.Failure("الدعوة غير صالحة.", "INVITATION_INVALID");
            }

            if (invitation.AcceptedAt.HasValue || invitation.RevokedAt.HasValue)
            {
                return ServiceResult.Failure("استُخدمت الدعوة أو أُلغيت بالفعل.", "INVITATION_ALREADY_USED");
            }

            if (invitation.ExpiresAt <= DateTime.UtcNow)
            {
                return ServiceResult.Failure("انتهت صلاحية الدعوة.", "INVITATION_EXPIRED");
            }

            if (!invitation.Tenant.IsActive)
            {
                return ServiceResult.Failure("حساب المستأجر غير نشط.", "TENANT_INACTIVE");
            }

            if (await _userManager.FindByEmailAsync(invitation.Email) != null)
            {
                return ServiceResult.Failure("الحساب موجود بالفعل.", "EMAIL_ALREADY_EXISTS");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await EnsureSystemRolesAsync();
                var user = new ApplicationUser
                {
                    UserName = invitation.Email,
                    Email = invitation.Email,
                    EmailConfirmed = true,
                    FullName = invitation.FullName,
                    TenantID = invitation.TenantID,
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult.Failure(
                        "تعذر إنشاء الحساب. تحقق من متطلبات كلمة المرور.",
                        "INVITATION_ACCEPTANCE_FAILED");
                }

                var roleResult = await _userManager.AddToRoleAsync(user, SystemRoles.TenantAdmin);
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult.Failure(
                        "تعذر إسناد دور مدير المستأجر.",
                        "INVITATION_ACCEPTANCE_FAILED");
                }

                invitation.AcceptedAt = DateTime.UtcNow;
                invitation.AcceptedByUserId = user.Id;
                invitation.UpdatedAt = DateTime.UtcNow;

                _dbContext.AuditLogs.Add(CreateAuditLog(
                    invitation.TenantID,
                    user.Id,
                    "TenantInvitation",
                    invitation.InvitationID.ToString(),
                    new
                    {
                        invitation.Email,
                        invitation.AcceptedAt,
                        AssignedRole = SystemRoles.TenantAdmin
                    },
                    "Accepted"));
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return ServiceResult.Failure(
                    "استُخدمت الدعوة بالتزامن من طلب آخر.",
                    "INVITATION_ALREADY_USED");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Invitation acceptance failed for invitation {InvitationId}.", invitation.InvitationID);
                return ServiceResult.Failure(
                    "تعذر قبول الدعوة.",
                    "INVITATION_ACCEPTANCE_FAILED");
            }
        }

        public async Task<ServiceResult> ResendInvitationAsync(
            int tenantId,
            string actorUserId)
        {
            var tenant = await _dbContext.Tenants.FindAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult.Failure("المستأجر غير موجود.", "TENANT_NOT_FOUND");
            }

            var previousInvitation = await _dbContext.TenantInvitations
                .Where(invitation =>
                    invitation.TenantID == tenantId &&
                    invitation.AcceptedAt == null)
                .OrderByDescending(invitation => invitation.CreatedAt)
                .FirstOrDefaultAsync();
            if (previousInvitation == null)
            {
                return ServiceResult.Failure("لا توجد دعوة قابلة لإعادة الإرسال.", "INVITATION_NOT_FOUND");
            }

            if (await _userManager.FindByEmailAsync(previousInvitation.Email) != null)
            {
                return ServiceResult.Failure("تم إنشاء الحساب بالفعل.", "INVITATION_ALREADY_USED");
            }

            var rawToken = TenantInvitationTokenService.GenerateToken();
            var now = DateTime.UtcNow;
            var replacement = new TenantInvitation
            {
                TenantID = tenantId,
                Email = previousInvitation.Email,
                NormalizedEmail = previousInvitation.NormalizedEmail,
                FullName = previousInvitation.FullName,
                TokenHash = TenantInvitationTokenService.Hash(rawToken),
                ExpiresAt = now.AddHours(GetInvitationExpiryHours()),
                CreatedByUserId = actorUserId
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var activeInvitations = await _dbContext.TenantInvitations
                    .Where(invitation =>
                        invitation.TenantID == tenantId &&
                        invitation.AcceptedAt == null &&
                        invitation.RevokedAt == null)
                    .ToListAsync();
                foreach (var activeInvitation in activeInvitations)
                {
                    activeInvitation.RevokedAt = now;
                    activeInvitation.UpdatedAt = now;
                }

                await _dbContext.SaveChangesAsync();
                _dbContext.TenantInvitations.Add(replacement);
                _dbContext.AuditLogs.Add(CreateAuditLog(
                    tenantId,
                    actorUserId,
                    "TenantInvitation",
                    replacement.InvitationID.ToString(),
                    new { replacement.Email, replacement.ExpiresAt },
                    "Resent"));
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Invitation resend failed for tenant {TenantId}.", tenantId);
                return ServiceResult.Failure(
                    "تعذر إعادة إنشاء الدعوة.",
                    "INVITATION_RESEND_FAILED");
            }

            try
            {
                await _invitationSender.SendAsync(
                    replacement.Email,
                    replacement.FullName,
                    tenant.CompanyName,
                    rawToken);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invitation email delivery failed for tenant {TenantId}.", tenantId);
                return ServiceResult.Failure(
                    "تم تجديد الدعوة، لكن تعذر إرسال البريد.",
                    "INVITATION_DELIVERY_FAILED");
            }
        }

        public async Task<ServiceResult<TenantDto>> UpdateAsync(int tenantId, TenantUpdateDto dto)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            var isUnique = await _unitOfWork.Tenants.IsSubdomainUniqueAsync(dto.Subdomain, tenantId);
            if (!isUnique)
            {
                return ServiceResult<TenantDto>.Failure("النطاق الفرعي مستخدم بالفعل", "SUBDOMAIN_ALREADY_EXISTS");
            }

            _mapper.Map(dto, tenant);
            tenant.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tenants.Update(tenant);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            // Using "System" as the default deleting user for service layer
            await _unitOfWork.Tenants.SoftDeleteAsync(tenantId, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<TenantSubscriptionDto>> GetActiveSubscriptionAsync(int tenantId)
        {
            var tenant = await _unitOfWork.Tenants.GetWithActiveSubscriptionAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantSubscriptionDto>.Failure("لا يوجد اشتراك نشط لهذا المستأجر", "ACTIVE_SUBSCRIPTION_NOT_FOUND");
            }

            var today = DateTime.UtcNow.Date;
            var activeSub = tenant.TenantSubscriptions
                .FirstOrDefault(s => (s.Status == Models.Enums.SubscriptionStatus.Active || s.Status == Models.Enums.SubscriptionStatus.Trial) &&
                                     s.StartDate.Date <= today &&
                                     s.EndDate.Date >= today);

            if (activeSub == null)
            {
                return ServiceResult<TenantSubscriptionDto>.Failure("لا يوجد اشتراك نشط لهذا المستأجر", "ACTIVE_SUBSCRIPTION_NOT_FOUND");
            }

            var dto = _mapper.Map<TenantSubscriptionDto>(activeSub);
            return ServiceResult<TenantSubscriptionDto>.Success(dto);
        }

        private int GetInvitationExpiryHours() =>
            Math.Clamp(_invitationOptions.ExpiryHours, 1, 168);

        private async Task EnsureSystemRolesAsync()
        {
            foreach (var roleName in SystemRoles.Required)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result = await _roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName
                });
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"ROLE_SETUP_FAILED:{roleName}");
                }
            }
        }

        private static AuditLog CreateAuditLog(
            int tenantId,
            string userId,
            string tableName,
            string recordId,
            object values,
            string action = "Created") => new()
        {
            TenantID = tenantId,
            UserId = userId,
            TableName = tableName,
            Action = action,
            ActionAt = DateTime.UtcNow,
            RecordID = recordId,
            IPAddress = "system",
            OldValues = string.Empty,
            NewValues = JsonSerializer.Serialize(values)
        };
    }
}
