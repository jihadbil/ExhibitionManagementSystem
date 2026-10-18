using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Implementations;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class AccessSessionTests
{
    [Fact]
    public async Task Login_BindsTokenToSessionAndLogoutRevokesIt()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        var actor = WorkflowFixture.Actor(invoice.TenantID);
        var services = new ServiceCollection().AddLogging().AddSingleton(db).AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor>(actor);
        services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ExhibitionManagementSystem.DataAccess.ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await db.Users.SingleAsync();
        user.Email = user.UserName = "workflow@example.test";
        Assert.True((await users.AddPasswordAsync(user, "TestPassword123!")).Succeeded);
        using var uow = new UnitOfWork(db, actor);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "test-only-signing-key-at-least-thirty-two-characters",
            ["JwtSettings:Issuer"] = "workflow-test", ["JwtSettings:Audience"] = "workflow-test",
            ["JwtSettings:AccessTokenExpiryMinutes"] = "15", ["JwtSettings:RefreshTokenExpiryDays"] = "7"
        }).Build();
        var service = new AuthService(users, provider.GetRequiredService<SignInManager<ApplicationUser>>(),
            provider.GetRequiredService<RoleManager<ApplicationRole>>(), config, WorkflowFixture.Mapper, uow, db, actor);
        var login = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "TestPassword123!" });
        Assert.True(login.IsSuccess, login.ErrorMessage);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.Data!.AccessToken);
        var sessionId = Guid.Parse(token.Claims.Single(c => c.Type == "session_id").Value);
        Assert.True(await db.RefreshSessions.AnyAsync(s => s.RefreshSessionID == sessionId && s.RevokedAt == null));
        var logout = await service.LogoutAsync(user.Id);
        Assert.True(logout.IsSuccess);
        Assert.True((await db.RefreshSessions.SingleAsync()).RevokedAt.HasValue);
        (await db.Tenants.SingleAsync()).IsActive = false;
        await db.SaveChangesAsync();
        var disabled = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "TestPassword123!" });
        Assert.False(disabled.IsSuccess);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("tenant")]
    [InlineData("session")]
    [InlineData("stamp")]
    [InlineData("roles")]
    public async Task AccessToken_RejectsRevokedIdentityOrPermissions(string change)
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        var user = await db.Users.SingleAsync();
        var session = new RefreshSession { UserId = user.Id, TenantID = invoice.TenantID, TokenHash = "test-hash", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        db.RefreshSessions.Add(session);
        await db.SaveChangesAsync();
        var services = new ServiceCollection().AddLogging().AddSingleton(db);
        services.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<ExhibitionManagementSystem.DataAccess.ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        var validator = new AccessTokenSessionValidator(db, provider.GetRequiredService<UserManager<ApplicationUser>>());
        var principal = WorkflowFixture.Actor(invoice.TenantID).HttpContext!.User;
        ((System.Security.Claims.ClaimsIdentity)principal.Identity!).AddClaims([
            new("session_id", session.RefreshSessionID.ToString()), new("security_stamp", user.SecurityStamp!)
        ]);
        Assert.True(await validator.ValidateAsync(principal));
        switch (change)
        {
            case "user": user.IsActive = false; break;
            case "tenant": (await db.Tenants.SingleAsync()).IsActive = false; break;
            case "session": session.RevokedAt = DateTime.UtcNow; break;
            case "stamp": user.SecurityStamp = "changed-stamp"; break;
            case "roles":
                var role = new ApplicationRole { Name = "Finance", NormalizedName = "FINANCE" };
                db.Roles.Add(role);
                db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
                break;
        }
        await db.SaveChangesAsync();
        Assert.False(await validator.ValidateAsync(principal));
    }
}
