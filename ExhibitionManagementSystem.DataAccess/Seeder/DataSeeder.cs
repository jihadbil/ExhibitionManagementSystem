using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExhibitionManagementSystem.DataAccess
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Apply any pending migrations
            await context.Database.MigrateAsync();

            // 1. Seed Currency
            if (!await context.Currencies.AnyAsync(c => c.CurrencyCode == "USD"))
            {
                context.Currencies.Add(new Currency
                {
                    CurrencyCode = "USD",
                    CurrencyName = "US Dollar",
                    Symbol = "$"
                });
                await context.SaveChangesAsync();
            }

            // 2. Seed Tenant
            var defaultTenant = await context.Tenants.FirstOrDefaultAsync(t => t.CompanyName == "System Admin");
            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    CompanyName = "System Admin",
                    Subdomain = "admin",
                    BaseCurrency = "USD",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Tenants.Add(defaultTenant);
                await context.SaveChangesAsync();

                var defaultSubscription = new TenantSubscription
                {
                    TenantID = defaultTenant.TenantID,
                    Plan = "Enterprise",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.AddYears(10).Date,
                    MonthlyFee = 0,
                    CurrencyCode = "USD",
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                context.TenantSubscriptions.Add(defaultSubscription);
                await context.SaveChangesAsync();
            }

            // 3. Seed Roles
            string adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = adminRole, TenantID = defaultTenant.TenantID });
            }

            // 4. Seed Admin User
            string adminEmail = "admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    TenantID = defaultTenant.TenantID,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                }
            }
        }
    }
}
