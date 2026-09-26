using ExhibitionManagementSystem.Controllers.Badges;
using ExhibitionManagementSystem.Controllers.Booth;
using ExhibitionManagementSystem.Controllers.Financial;
using ExhibitionManagementSystem.Controllers.Sponsorship;
using ExhibitionManagementSystem.Controllers.Visitor;
using ExhibitionManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public class AuthorizationPolicyTests
{
    [Fact]
    public void TenantPolicies_ExcludeSuperAdminFromOperationalRoles()
    {
        var options = new AuthorizationOptions();
        AuthorizationPolicies.Configure(options);

        foreach (var policyName in new[]
                 {
                     AuthorizationPolicies.TenantAdministration,
                     AuthorizationPolicies.EventManagement,
                     AuthorizationPolicies.FinanceManagement,
                     AuthorizationPolicies.GateOperations,
                     AuthorizationPolicies.SponsorshipManagement,
                     AuthorizationPolicies.BadgeManagement
                 })
        {
            var policy = options.GetPolicy(policyName);
            var roles = Assert.Single(policy!.Requirements.OfType<RolesAuthorizationRequirement>())
                .AllowedRoles;
            Assert.DoesNotContain("SuperAdmin", roles);
        }
    }

    [Theory]
    [InlineData(typeof(SponsorshipController), AuthorizationPolicies.SponsorshipManagement)]
    [InlineData(typeof(BadgesController), AuthorizationPolicies.BadgeManagement)]
    [InlineData(typeof(ReservationsController), AuthorizationPolicies.EventManagement)]
    [InlineData(typeof(FinancialController), AuthorizationPolicies.FinanceManagement)]
    [InlineData(typeof(TicketsController), AuthorizationPolicies.GateOperations)]
    public void SensitiveControllers_RequireExpectedPolicy(Type controllerType, string expectedPolicy)
    {
        var policies = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy);

        Assert.Contains(expectedPolicy, policies);
    }

    [Fact]
    public void BoothDelete_MatchesDesktopContractAndRequiresAdministration()
    {
        var action = typeof(BoothsController).GetMethod(nameof(BoothsController.Delete));

        Assert.NotNull(action);
        Assert.Equal(
            "{boothId:int}",
            Assert.Single(action.GetCustomAttributes(typeof(HttpDeleteAttribute), false)
                .Cast<HttpDeleteAttribute>()).Template);
        Assert.Contains(
            action.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>(),
            attribute => attribute.Policy == "AdminOnly");
    }
}
