using ExhibitionManagementSystem.Api.Tests.Regression;
using ExhibitionManagementSystem.DataAccess.Seeder;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class DataSeederSafetyTests
{
    [Fact]
    public async Task StartupSeedingPreservesExistingCredentialsActivationAndRoles()
    {
        using var db = WorkflowFixture.CreateDb();
        var services = new ServiceCollection().AddLogging().AddSingleton(db);
        services.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<ExhibitionManagementSystem.DataAccess.ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        await DataSeeder.SeedInitialDataAsync(provider);
        Assert.Empty(await db.Users.ToListAsync());
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "masoud", Email = "masoud@exhibition.ly", FullName = "Existing user",
            TenantID = (await db.Tenants.SingleAsync()).TenantID, IsActive = false };
        Assert.True((await users.CreateAsync(user, "Existing123!Password")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, SystemRoles.Finance)).Succeeded);
        var hash = user.PasswordHash;
        var stamp = user.SecurityStamp;
        await DataSeeder.SeedInitialDataAsync(provider);
        Assert.Equal(hash, user.PasswordHash);
        Assert.Equal(stamp, user.SecurityStamp);
        Assert.False(user.IsActive);
        Assert.Equal(new[] { SystemRoles.Finance }, await users.GetRolesAsync(user));

        await DataSeeder.BootstrapAdminAsync(provider, new BootstrapAdminOptions { Enabled = true, UserName = user.UserName!,
            Email = user.Email!, Password = "Other123!Password", TenantID = user.TenantID });
        Assert.Equal(hash, user.PasswordHash);
        Assert.False(user.IsActive);
        Assert.Equal(new[] { SystemRoles.Finance }, await users.GetRolesAsync(user));
    }

    [Fact]
    public async Task BootstrapIsDisabledByDefaultAndOnlyCreatesTheFirstAdministrator()
    {
        using var db = WorkflowFixture.CreateDb();
        var services = new ServiceCollection().AddLogging().AddSingleton(db);
        services.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<ExhibitionManagementSystem.DataAccess.ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        await DataSeeder.SeedInitialDataAsync(provider);
        await DataSeeder.BootstrapAdminAsync(provider, new BootstrapAdminOptions());
        Assert.Empty(await db.Users.ToListAsync());
        var options = new BootstrapAdminOptions { Enabled = true, UserName = "initial-admin", Email = "initial@example.invalid",
            Password = "Initial123!Password", TenantID = (await db.Tenants.SingleAsync()).TenantID };
        await DataSeeder.BootstrapAdminAsync(provider, options);
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await db.Users.SingleAsync();
        Assert.Equal(new[] { SystemRoles.SuperAdmin }, await users.GetRolesAsync(user));
        var hash = user.PasswordHash;
        options.UserName = "second-admin"; options.Email = "second@example.invalid";
        await DataSeeder.BootstrapAdminAsync(provider, options);
        Assert.Single(await db.Users.ToListAsync());
        Assert.Equal(hash, user.PasswordHash);
    }
}
