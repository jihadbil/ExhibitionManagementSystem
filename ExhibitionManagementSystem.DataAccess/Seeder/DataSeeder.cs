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
            bool hasUSD = await context.Currencies.AnyAsync(c => c.CurrencyCode == "USD");
            if (!hasUSD)
            {
                context.Currencies.Add(new Currency
                {
                    CurrencyCode = "USD",
                    CurrencyName = "US Dollar",
                    Symbol = "$"
                });
            }

            bool hasLYD = await context.Currencies.AnyAsync(c => c.CurrencyCode == "LYD");
            if (!hasLYD)
            {
                context.Currencies.Add(new Currency
                {
                    CurrencyCode = "LYD",
                    CurrencyName = "Libyan Dinar",
                    Symbol = "د.ل"
                });
            }

            bool hasEUR = await context.Currencies.AnyAsync(c => c.CurrencyCode == "EUR");
            if (!hasEUR)
            {
                context.Currencies.Add(new Currency
                {
                    CurrencyCode = "EUR",
                    CurrencyName = "Euro",
                    Symbol = "€"
                });
            }

            if (!hasUSD || !hasLYD || !hasEUR)
            {
                await context.SaveChangesAsync();
            }

            // Seed Exchange Rates
            if (!await context.ExchangeRates.AnyAsync(r => r.FromCurrency == "USD" && r.ToCurrency == "LYD"))
            {
                context.ExchangeRates.Add(new ExchangeRate
                {
                    FromCurrency = "USD",
                    ToCurrency = "LYD",
                    Rate = 4.80m,
                    RateDate = DateTime.UtcNow.Date,
                    Source = "System Init",
                    CreatedAt = DateTime.UtcNow
                });
            }
            if (!await context.ExchangeRates.AnyAsync(r => r.FromCurrency == "LYD" && r.ToCurrency == "USD"))
            {
                context.ExchangeRates.Add(new ExchangeRate
                {
                    FromCurrency = "LYD",
                    ToCurrency = "USD",
                    Rate = 0.2083m,
                    RateDate = DateTime.UtcNow.Date,
                    Source = "System Init",
                    CreatedAt = DateTime.UtcNow
                });
            }
            if (!await context.ExchangeRates.AnyAsync(r => r.FromCurrency == "USD" && r.ToCurrency == "EUR"))
            {
                context.ExchangeRates.Add(new ExchangeRate
                {
                    FromCurrency = "USD",
                    ToCurrency = "EUR",
                    Rate = 0.92m,
                    RateDate = DateTime.UtcNow.Date,
                    Source = "System Init",
                    CreatedAt = DateTime.UtcNow
                });
            }
            if (!await context.ExchangeRates.AnyAsync(r => r.FromCurrency == "EUR" && r.ToCurrency == "USD"))
            {
                context.ExchangeRates.Add(new ExchangeRate
                {
                    FromCurrency = "EUR",
                    ToCurrency = "USD",
                    Rate = 1.087m,
                    RateDate = DateTime.UtcNow.Date,
                    Source = "System Init",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

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
