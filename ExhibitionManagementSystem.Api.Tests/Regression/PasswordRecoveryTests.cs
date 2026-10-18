using ExhibitionManagementSystem.Controllers.Auth;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class PasswordRecoveryTests
{
    private sealed class Sender : IPasswordResetSender
    {
        public string? Email { get; private set; }
        public string? Token { get; private set; }
        public bool Fail { get; set; }
        public Task SendAsync(string email, string fullName, string token, CancellationToken cancellationToken = default)
        {
            Email = email; Token = token;
            if (Fail) throw new InvalidOperationException("Synthetic delivery failure");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RecoveryDeliversUsableSingleUseTokenAndRevokesSessions()
    {
        using var db = WorkflowFixture.CreateDb();
        var fixture = await WorkflowFixture.SeedAsync(db);
        var actor = WorkflowFixture.Actor(fixture.TenantID);
        var services = new ServiceCollection().AddLogging().AddSingleton(db)
            .AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor>(actor);
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        using var provider = services.BuildServiceProvider();
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await db.Users.SingleAsync();
        user.Email = user.UserName = "recovery@example.test";
        Assert.True((await users.UpdateAsync(user)).Succeeded);
        Assert.True((await users.AddPasswordAsync(user, "Original123!Password")).Succeeded);
        db.RefreshSessions.Add(new RefreshSession { UserId = user.Id, TenantID = fixture.TenantID,
            TokenHash = "synthetic-hash", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, actor);
        var sender = new Sender();
        var auth = new AuthService(users, provider.GetRequiredService<SignInManager<ApplicationUser>>(),
            provider.GetRequiredService<RoleManager<ApplicationRole>>(), new ConfigurationBuilder().Build(),
            WorkflowFixture.Mapper, uow, db, actor, sender);
        var request = await auth.ForgotPasswordAsync(new ResetPasswordRequestDto { Email = user.Email });
        Assert.True(request.IsSuccess);
        Assert.Null(request.ErrorMessage);
        Assert.Equal(user.Email, sender.Email);
        Assert.False(string.IsNullOrEmpty(sender.Token));
        var dto = new ResetPasswordConfirmDto { Email = user.Email, Token = sender.Token!,
            NewPassword = "Replacement123!Password", ConfirmPassword = "Replacement123!Password" };
        Assert.True((await auth.ResetPasswordAsync(dto)).IsSuccess);
        Assert.True(await users.CheckPasswordAsync(user, dto.NewPassword));
        Assert.False(await users.CheckPasswordAsync(user, "Original123!Password"));
        Assert.NotNull((await db.RefreshSessions.SingleAsync()).RevokedAt);
        Assert.False((await auth.ResetPasswordAsync(dto)).IsSuccess);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecoveryDoesNotExposeMissingAccountsOrDeliveryErrors(bool registered)
    {
        using var db = WorkflowFixture.CreateDb();
        var fixture = await WorkflowFixture.SeedAsync(db);
        var actor = WorkflowFixture.Actor(fixture.TenantID);
        var services = new ServiceCollection().AddLogging().AddSingleton(db)
            .AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor>(actor);
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        using var provider = services.BuildServiceProvider();
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await db.Users.SingleAsync(); user.Email = user.UserName = "known@example.test";
        Assert.True((await users.UpdateAsync(user)).Succeeded);
        using var uow = new UnitOfWork(db, actor);
        var sender = new Sender { Fail = true };
        var auth = new AuthService(users, provider.GetRequiredService<SignInManager<ApplicationUser>>(),
            provider.GetRequiredService<RoleManager<ApplicationRole>>(), new ConfigurationBuilder().Build(),
            WorkflowFixture.Mapper, uow, db, actor, sender);
        var controller = new AuthController(auth, null!);
        var response = await controller.ForgotPassword(new ResetPasswordRequestDto { Email = registered ? user.Email : "missing@example.test" });
        Assert.IsType<OkObjectResult>(response);
        Assert.Equal(registered, sender.Token != null);
    }
}
