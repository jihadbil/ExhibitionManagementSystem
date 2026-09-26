using System.Reflection;
using ExhibitionManagementSystem.Controllers.Auth;
using ExhibitionManagementSystem.Controllers.Visitor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class AuthEndpointSecurityTests
{
    [Fact]
    public void AuthController_DoesNotExposeAnonymousUserRegistration()
    {
        var anonymousPostRoutes = typeof(AuthController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .SelectMany(method => method.GetCustomAttributes<HttpPostAttribute>())
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.DoesNotContain(
            anonymousPostRoutes,
            route => string.Equals(route, "register", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AuthController_IsProtectedByRateLimitingPolicy()
    {
        var attribute = typeof(AuthController)
            .GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("authentication", attribute.PolicyName);
    }

    [Fact]
    public void VisitorRegistration_RequiresAuthenticatedTenantContext()
    {
        var register = typeof(VisitorsController).GetMethod(nameof(VisitorsController.Register));

        Assert.NotNull(register);
        Assert.Null(register.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
