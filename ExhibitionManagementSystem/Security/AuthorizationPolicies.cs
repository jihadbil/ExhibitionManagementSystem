using Microsoft.AspNetCore.Authorization;
using ExhibitionManagementSystem.Models.Security;

namespace ExhibitionManagementSystem.Security;

/// <summary>
/// Central authorization policy names and their backward-compatible role mappings.
/// SuperAdmin is intentionally excluded from tenant operational policies.
/// </summary>
public static class AuthorizationPolicies
{
    public const string TenantAdministration = "TenantAdministration";
    public const string EventManagement = "EventManagement";
    public const string FinanceManagement = "FinanceManagement";
    public const string GateOperations = "GateOperations";
    public const string SponsorshipManagement = "SponsorshipManagement";
    public const string BadgeManagement = "BadgeManagement";
    public const string SuperAdminOnly = "SuperAdminOnly";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole(SystemRoles.Admin, SystemRoles.TenantAdmin));
        options.AddPolicy(TenantAdministration, policy =>
            policy.RequireRole(SystemRoles.Admin, SystemRoles.TenantAdmin));
        options.AddPolicy(EventManagement, policy =>
            policy.RequireRole(SystemRoles.Admin, SystemRoles.TenantAdmin, SystemRoles.EventManager));
        options.AddPolicy(FinanceManagement, policy =>
            policy.RequireRole(SystemRoles.Admin, SystemRoles.TenantAdmin, SystemRoles.Finance));
        options.AddPolicy(GateOperations, policy =>
            policy.RequireRole(SystemRoles.Admin, SystemRoles.TenantAdmin, SystemRoles.EventManager, SystemRoles.GateOperator));
        options.AddPolicy(SponsorshipManagement, policy =>
            policy.RequireRole(SystemRoles.Admin, SystemRoles.TenantAdmin, SystemRoles.EventManager));
        options.AddPolicy(BadgeManagement, policy =>
            policy.RequireRole(SystemRoles.Admin, SystemRoles.TenantAdmin, SystemRoles.EventManager));
        options.AddPolicy(SuperAdminOnly, policy =>
            policy.RequireRole(SystemRoles.SuperAdmin));
    }
}
