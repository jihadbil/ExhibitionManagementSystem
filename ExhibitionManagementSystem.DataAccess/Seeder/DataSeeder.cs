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
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
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
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // 4. Seed Super User (masoud) with full permissions
            const string targetUserName = "masoud";
            const string targetEmail = "masoud@exhibition.ly";
            const string targetPassword = "9713563@#Aa";

            var adminUser = await userManager.FindByNameAsync(targetUserName) 
                         ?? await userManager.FindByEmailAsync(targetEmail)
                         ?? await userManager.FindByEmailAsync(targetUserName);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = targetUserName,
                    Email = targetEmail,
                    FullName = "Masoud",
                    TenantID = defaultTenant.TenantID,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, targetPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRolesAsync(adminUser, requiredRoles);
                    logger.LogInformation("Seeded initial user '{UserName}' with full system permissions.", targetUserName);
                }
                else
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create user '{UserName}': {Errors}", targetUserName, errors);
                    throw new InvalidOperationException($"Failed to seed user '{targetUserName}': {errors}");
                }
            }
            else
            {
                // Ensure password matches target password
                if (!await userManager.CheckPasswordAsync(adminUser, targetPassword))
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var resetResult = await userManager.ResetPasswordAsync(adminUser, resetToken, targetPassword);
                    if (resetResult.Succeeded)
                    {
                        logger.LogInformation("Updated password for user '{UserName}'.", targetUserName);
                    }
                }

                // Ensure all roles are assigned
                var userRoles = await userManager.GetRolesAsync(adminUser);
                var missingRoles = requiredRoles.Except(userRoles, StringComparer.OrdinalIgnoreCase).ToList();
                if (missingRoles.Count > 0)
                {
                    await userManager.AddToRolesAsync(adminUser, missingRoles);
                    logger.LogInformation("Assigned missing roles to user '{UserName}': {Roles}", targetUserName, string.Join(", ", missingRoles));
                }

                // Ensure user is active and linked to tenant
                bool modified = false;
                if (!adminUser.IsActive)
                {
                    adminUser.IsActive = true;
                    modified = true;
                }
                if (adminUser.TenantID == 0)
                {
                    adminUser.TenantID = defaultTenant.TenantID;
                    modified = true;
                }
                if (modified)
                {
                    await userManager.UpdateAsync(adminUser);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during initial data seeding.");
            throw;
        }
    }
}
