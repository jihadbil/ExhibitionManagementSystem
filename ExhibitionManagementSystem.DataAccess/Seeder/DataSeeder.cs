using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExhibitionManagementSystem.DataAccess.Seeder;

public static class DataSeeder
{
    public static async Task SeedInitialDataAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        try
        {
            // 1. Seed Base Currency (LYD)
            if (!await dbContext.Currencies.AnyAsync(c => c.CurrencyCode == "LYD"))
            {
                dbContext.Currencies.Add(new Currency
                {
                    CurrencyCode = "LYD",
                    CurrencyName = "دينار ليبي",
                    Symbol = "د.ل",
                    IsActive = true
                });
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Seeded default currency: LYD");
            }

            // 2. Seed Default Tenant
            var defaultTenant = await dbContext.Tenants.FirstOrDefaultAsync();
            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    CompanyName = "الهيئة العامة للمعارض",
                    Subdomain = "main",
                    BaseCurrency = "LYD",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Tenants.Add(defaultTenant);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Seeded default tenant: {CompanyName}", defaultTenant.CompanyName);
            }

            // 3. Seed Required System Roles
            var requiredRoles = new[]
            {
                SystemRoles.SuperAdmin,
                SystemRoles.Admin,
                SystemRoles.TenantAdmin,
                SystemRoles.EventManager,
                SystemRoles.Finance,
                SystemRoles.GateOperator
            };

            foreach (var roleName in requiredRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var created = await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                        CreatedAt = DateTime.UtcNow
                    });
                    if (!created.Succeeded)
                        throw new InvalidOperationException("System role creation failed: " + string.Join(", ", created.Errors.Select(e => e.Code)));
                }
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during initial data seeding.");
            throw;
        }
    }

    public static async Task BootstrapAdminAsync(IServiceProvider serviceProvider, BootstrapAdminOptions options)
    {
        if (!options.Enabled)
            return;

        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var users = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.Email) ||
            string.IsNullOrWhiteSpace(options.Password) || options.TenantID <= 0)
            throw new InvalidOperationException("BootstrapAdmin requires UserName, Email, Password and a valid TenantID.");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
            : null;

        // Explicit bootstrap only creates the first administrator. Existing accounts
        // keep their passwords, activation state, tenant and assigned permissions.
        if (await users.FindByNameAsync(options.UserName.Trim()) != null ||
            await users.FindByEmailAsync(options.Email.Trim()) != null ||
            (await users.GetUsersInRoleAsync(SystemRoles.SuperAdmin)).Count > 0)
        {
            logger.LogInformation("Administrator bootstrap skipped because an account or administrator already exists.");
            return;
        }

        if (!await db.Tenants.AnyAsync(t => t.TenantID == options.TenantID && t.IsActive))
            throw new InvalidOperationException("BootstrapAdmin requires an existing active tenant.");

        var user = new ApplicationUser
        {
            UserName = options.UserName.Trim(), Email = options.Email.Trim(),
            FullName = string.IsNullOrWhiteSpace(options.FullName) ? options.UserName.Trim() : options.FullName.Trim(),
            TenantID = options.TenantID, IsActive = true, EmailConfirmed = true
        };
        var created = await users.CreateAsync(user, options.Password);
        if (!created.Succeeded)
            throw new InvalidOperationException("Administrator bootstrap failed: " + string.Join(", ", created.Errors.Select(e => e.Code)));
        var assigned = await users.AddToRoleAsync(user, SystemRoles.SuperAdmin);
        if (!assigned.Succeeded)
            throw new InvalidOperationException("Administrator role assignment failed: " + string.Join(", ", assigned.Errors.Select(e => e.Code)));
        if (transaction != null)
            await transaction.CommitAsync();
        logger.LogInformation("Created the initial platform administrator through explicit bootstrap configuration.");
    }
}
