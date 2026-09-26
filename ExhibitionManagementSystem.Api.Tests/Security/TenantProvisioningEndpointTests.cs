using System.Reflection;
using ExhibitionManagementSystem.Controllers.Admin;
using ExhibitionManagementSystem.Controllers.Auth;
using ExhibitionManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class TenantProvisioningEndpointTests
{
    [Fact]
    public void TenantAdministration_IsRestrictedToSuperAdmin()
    {
        var authorize = typeof(TenantsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy is not null);

        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.SuperAdminOnly, authorize.Policy);
    }

    [Fact]
    public void OnlyInvitationAcceptance_IsAnonymous()
    {
        var accept = typeof(TenantsController)
            .GetMethod(nameof(TenantsController.AcceptInvitation));
        var create = typeof(TenantsController)
            .GetMethod(nameof(TenantsController.Create));
        var resend = typeof(TenantsController)
            .GetMethod(nameof(TenantsController.ResendInvitation));

        Assert.NotNull(accept?.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(create?.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(resend?.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void InvitationAcceptancePage_IsAnonymous()
    {
        var page = typeof(TenantInvitationPageController)
            .GetMethod(nameof(TenantInvitationPageController.Index));

        Assert.NotNull(page?.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
