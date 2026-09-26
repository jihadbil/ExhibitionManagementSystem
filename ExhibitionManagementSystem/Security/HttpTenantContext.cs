using System.Security.Claims;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;

namespace ExhibitionManagementSystem.Security;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? TenantId
    {
        get
        {
            var value = User?.FindFirstValue("TenantId");
            return int.TryParse(value, out var tenantId) && tenantId > 0
                ? tenantId
                : null;
        }
    }

    public bool CanBypassTenantIsolation => User?.IsInRole("SuperAdmin") == true;
}
