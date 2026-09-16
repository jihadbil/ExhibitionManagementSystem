using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExhibitionManagementSystem.DataAccess
{
    public static class DataSeederTest
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // 1. Find or create target tenant (TenantID = 1, or fallback to the first one)
            var tenant1 = await context.Tenants.FirstOrDefaultAsync(t => t.TenantID == 1)
                          ?? await context.Tenants.FirstOrDefaultAsync();

            if (tenant1 == null)
            {
                tenant1 = new Tenant
                {
                    CompanyName = "System Tenant 1",
                    Subdomain = "techorg",
                    BaseCurrency = "USD",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Tenants.Add(tenant1);
                await context.SaveChangesAsync();
            }

            var tenant2 = tenant1;
            int targetTenantId = tenant1.TenantID;

            // 1b. Targeted Cleanup of seeded test data under target tenant to allow fresh re-seeding
            var testEmails = new[] { "admin@techorg.com", "admin@gulfexpo.com", "exhibitor-user@example.com" };
            var usersToDelete = await context.Users.Where(u => testEmails.Contains(u.Email)).ToListAsync();
            var userIds = usersToDelete.Select(u => u.Id).ToList();

            var testExhibitionsNames = new[] 
            { 
                "Tripoli Tech Summit 2026", 
                "Libya International Book Fair", 
                "Dubai AI & Cloud Expo 2026", 
                "Riyadh Real Estate & Construction Fair", 
                "Gulf Medical Devices Show 2025" 
            };

            // Get test exhibitions
            var exhibitionsToDelete = await context.Exhibitions
                .Where(e => e.TenantID == targetTenantId && testExhibitionsNames.Contains(e.Name))
                .ToListAsync();
            var exhibitionIds = exhibitionsToDelete.Select(e => e.ExhibitionID).ToList();

            // Get reservations for these exhibitions or created by test users
            var reservationsToDelete = await context.BoothReservations
                .Where(r => exhibitionIds.Contains(r.ExhibitionID) || userIds.Contains(r.CreatedByUserId))
                .ToListAsync();
            var reservationIds = reservationsToDelete.Select(r => r.ReservationID).ToList();

            // Get invoices for these reservations
            var invoicesToDelete = await context.Invoices
                .Where(i => reservationIds.Contains(i.ReservationID))
                .ToListAsync();
            var invoiceIds = invoicesToDelete.Select(i => i.InvoiceID).ToList();

            // 1. Payments
            var paymentsToDelete = await context.Payments.Where(p => invoiceIds.Contains(p.InvoiceID)).ToListAsync();
            context.Payments.RemoveRange(paymentsToDelete);

            // 2. InvoiceItems
            var itemsToDelete = await context.InvoiceItems.Where(i => invoiceIds.Contains(i.InvoiceID)).ToListAsync();
            context.InvoiceItems.RemoveRange(itemsToDelete);

            // 3. Invoices
            context.Invoices.RemoveRange(invoicesToDelete);

            // 4. BoothStaffs
            var staffToDelete = await context.BoothStaffs.Where(s => reservationIds.Contains(s.ReservationID)).ToListAsync();
            context.BoothStaffs.RemoveRange(staffToDelete);

            // 5. ReservationServices
            var resServicesToDelete = await context.ReservationServices.Where(rs => reservationIds.Contains(rs.ReservationID)).ToListAsync();
            context.ReservationServices.RemoveRange(resServicesToDelete);

            // 6. BoothReservations
            context.BoothReservations.RemoveRange(reservationsToDelete);

            // 7. VisitorRatings
            var ratingsToDelete = await context.VisitorRatings.Where(r => exhibitionIds.Contains(r.ExhibitionID)).ToListAsync();
            context.VisitorRatings.RemoveRange(ratingsToDelete);

            // 8. Expenses
            var expensesToDelete = await context.Expenses.Where(e => exhibitionIds.Contains(e.ExhibitionID)).ToListAsync();
            context.Expenses.RemoveRange(expensesToDelete);

            // 9. Products
            var productsToDelete = await context.Products.Where(p => exhibitionIds.Contains(p.ExhibitionID)).ToListAsync();
            context.Products.RemoveRange(productsToDelete);

            // 10. TicketScans (associated with test exhibitions or test tickets)
            var scansToDelete = await context.TicketScans
                .Where(s => exhibitionIds.Contains(s.Ticket.ExhibitionID) || s.Ticket.QRCode.StartsWith("TICKET-QR-"))
                .ToListAsync();
            context.TicketScans.RemoveRange(scansToDelete);

            // 11. Tickets
            var ticketsToDelete = await context.Tickets.Where(t => exhibitionIds.Contains(t.ExhibitionID) || t.QRCode.StartsWith("TICKET-QR-")).ToListAsync();
            context.Tickets.RemoveRange(ticketsToDelete);

            // 12. ExhibitionSchedules
            var schedulesToDelete = await context.ExhibitionSchedules.Where(s => exhibitionIds.Contains(s.ExhibitionID)).ToListAsync();
            context.ExhibitionSchedules.RemoveRange(schedulesToDelete);

            // 13. ServicePriceRules
            var serviceRulesToDelete = await context.ServicePriceRules.Where(r => r.ExhibitionID != null && exhibitionIds.Contains(r.ExhibitionID.Value)).ToListAsync();
            context.ServicePriceRules.RemoveRange(serviceRulesToDelete);

            // 14. Exhibitions
            context.Exhibitions.RemoveRange(exhibitionsToDelete);

            // 15. Venues, Halls, Booths
            var testVenuesNames = new[] { "Tripoli Convention Center", "Dubai World Trade Centre", "Riyadh Exhibition & Convention Center" };
            var venuesToDelete = await context.Venues.Where(v => v.TenantID == targetTenantId && testVenuesNames.Contains(v.Name)).ToListAsync();
            var venueIds = venuesToDelete.Select(v => v.VenueID).ToList();

            var hallsToDelete = await context.Halls.Where(h => venueIds.Contains(h.VenueID)).ToListAsync();
            var hallIds = hallsToDelete.Select(h => h.HallID).ToList();

            var boothsToDelete = await context.Booths.Where(b => hallIds.Contains(b.HallID)).ToListAsync();
            context.Booths.RemoveRange(boothsToDelete);
            context.Halls.RemoveRange(hallsToDelete);
            context.Venues.RemoveRange(venuesToDelete);

            // 16. Exhibitors
            var testExhibitorNames = new[] { "LTT - Libya Telecom", "Al-Madar Al-Jadid", "Oracle Libya", "Microsoft Gulf", "AWS Middle East", "Saudi Aramco", "Emaar Properties", "Riyadh Municipality", "Dubai Health Authority" };
            var exhibitorsToDelete = await context.Exhibitors.Where(e => e.TenantID == targetTenantId && testExhibitorNames.Contains(e.CompanyName)).ToListAsync();
            context.Exhibitors.RemoveRange(exhibitorsToDelete);

            // 17. Services
            var testServiceNames = new[] { "High-Speed WiFi", "3-Phase Power Connection", "Catering Service", "Daily Stand Cleaning", "Premium Lounge Furniture Set", "Extra Spotlights Bundle", "Exhibition Guidebook Ads Placement" };
            var servicesToDelete = await context.Services.Where(s => s.TenantID == targetTenantId && testServiceNames.Contains(s.ServiceName)).ToListAsync();
            context.Services.RemoveRange(servicesToDelete);

            // 18. BoothPriceRules
            var testRuleNames = new[] { "Local Tech Rule", "International Tech Rule", "Gulf Premium Rule", "Gulf Local Rule", "Saudi Govt Rule" };
            var priceRulesToDelete = await context.BoothPriceRules.Where(r => r.TenantID == targetTenantId && testRuleNames.Contains(r.RuleName)).ToListAsync();
            context.BoothPriceRules.RemoveRange(priceRulesToDelete);

            // 19. AuditLogs (created by test users - regardless of TenantID)
            var logsToDelete = await context.AuditLogs.Where(l => userIds.Contains(l.UserId)).ToListAsync();
            context.AuditLogs.RemoveRange(logsToDelete);

            // Nullify user references in other tables before user deletion
            var exhibitorsToNullify = await context.Exhibitors.Where(e => userIds.Contains(e.UserId)).ToListAsync();
            foreach (var exhibitor in exhibitorsToNullify)
            {
                exhibitor.UserId = null;
            }

            var paymentsToNullify = await context.Payments.Where(p => userIds.Contains(p.ReceivedByUserId)).ToListAsync();
            context.Payments.RemoveRange(paymentsToNullify);

            var scansToNullify = await context.TicketScans.Where(s => userIds.Contains(s.ScannedByUserId)).ToListAsync();
            context.TicketScans.RemoveRange(scansToNullify);

            var expensesToNullify = await context.Expenses.Where(e => userIds.Contains(e.CreatedByUserId)).ToListAsync();
            context.Expenses.RemoveRange(expensesToNullify);

            // 20. ExchangeRates
            var ratesToDelete = await context.ExchangeRates.Where(r => r.Source == "Seeder Test").ToListAsync();
            context.ExchangeRates.RemoveRange(ratesToDelete);

            // 21. Visitors
            var visitorEmails = Enumerable.Range(1, 25).Select(i => $"visitor{i}@").ToList();
            var visitorsToDelete = await context.Visitors.Where(v => v.TenantID == targetTenantId).ToListAsync();
            visitorsToDelete = visitorsToDelete.Where(v => v.Email != null && visitorEmails.Any(pattern => v.Email.StartsWith(pattern))).ToList();
            context.Visitors.RemoveRange(visitorsToDelete);

            await context.SaveChangesAsync();

            // Safely delete users now that all referencing records have been deleted or nullified.
            foreach (var user in usersToDelete)
            {
                await userManager.DeleteAsync(user);
            }

            // 4. Tenant Subscription (create only if none exists for target tenant)
            bool hasSub = await context.TenantSubscriptions.AnyAsync(s => s.TenantID == targetTenantId);
            if (!hasSub)
            {
                var sub1 = new TenantSubscription
                {
                    TenantID = targetTenantId,
                    Plan = "Enterprise",
                    StartDate = DateTime.UtcNow.AddMonths(-1).Date,
                    EndDate = DateTime.UtcNow.AddYears(1).Date,
                    MonthlyFee = 500.00m,
                    CurrencyCode = "USD",
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                context.TenantSubscriptions.Add(sub1);
                await context.SaveChangesAsync();
            }

            // 5. Seed Identity Roles
            string adminRoleName = "Admin";
            string exhibitorRoleName = "Exhibitor";

            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = adminRoleName, TenantID = tenant1.TenantID });
            }
            if (!await roleManager.RoleExistsAsync(exhibitorRoleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = exhibitorRoleName, TenantID = tenant1.TenantID });
            }

            // 6. Seed Test Users
            var hasher = new PasswordHasher<ApplicationUser>();

            var user1 = new ApplicationUser
            {
                UserName = "admin@techorg.com",
                Email = "admin@techorg.com",
                FullName = "TechOrg Administrator",
                TenantID = tenant1.TenantID,
                EmailConfirmed = true,
                IsActive = true
            };
            user1.PasswordHash = hasher.HashPassword(user1, "Admin@123");
            await userManager.CreateAsync(user1);
            await userManager.AddToRoleAsync(user1, adminRoleName);

            var user2 = new ApplicationUser
            {
                UserName = "admin@gulfexpo.com",
                Email = "admin@gulfexpo.com",
                FullName = "GulfExpo Administrator",
                TenantID = tenant2.TenantID,
                EmailConfirmed = true,
                IsActive = true
            };
            user2.PasswordHash = hasher.HashPassword(user2, "Admin@123");
            await userManager.CreateAsync(user2);
            await userManager.AddToRoleAsync(user2, adminRoleName);

            var user3 = new ApplicationUser
            {
                UserName = "exhibitor-user@example.com",
                Email = "exhibitor-user@example.com",
                FullName = "Gulf Exhibitor Rep",
                TenantID = tenant2.TenantID,
                EmailConfirmed = true,
                IsActive = true
            };
            user3.PasswordHash = hasher.HashPassword(user3, "Exhibitor@123");
            await userManager.CreateAsync(user3);
            await userManager.AddToRoleAsync(user3, exhibitorRoleName);

            // 7. Seed Exchange Rates
            var exchangeRates = new List<ExchangeRate>
            {
                new ExchangeRate { FromCurrency = "USD", ToCurrency = "AED", Rate = 3.6725m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },
                new ExchangeRate { FromCurrency = "AED", ToCurrency = "USD", Rate = 0.2723m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },
                new ExchangeRate { FromCurrency = "USD", ToCurrency = "SAR", Rate = 3.7500m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },
                new ExchangeRate { FromCurrency = "SAR", ToCurrency = "USD", Rate = 0.2667m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },
                new ExchangeRate { FromCurrency = "AED", ToCurrency = "SAR", Rate = 1.0210m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user2.Id, CreatedAt = DateTime.UtcNow },
                new ExchangeRate { FromCurrency = "SAR", ToCurrency = "AED", Rate = 0.9790m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user2.Id, CreatedAt = DateTime.UtcNow },
                new ExchangeRate { FromCurrency = "USD", ToCurrency = "QAR", Rate = 3.6415m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },
                new ExchangeRate { FromCurrency = "USD", ToCurrency = "EGP", Rate = 48.5000m, RateDate = DateTime.UtcNow.Date, Source = "Seeder Test", CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow }
            };

            foreach (var rate in exchangeRates)
            {
                bool exists = await context.ExchangeRates.AnyAsync(r => r.FromCurrency == rate.FromCurrency && r.ToCurrency == rate.ToCurrency && r.RateDate == rate.RateDate);
                if (!exists)
                {
                    context.ExchangeRates.Add(rate);
                }
            }
            await context.SaveChangesAsync();

            // 8. Seed Venues & Halls
            var venue1 = new Venue
            {
                TenantID = tenant1.TenantID,
                Name = "Tripoli Convention Center",
                Address = "Airport Road",
                City = "Tripoli",
                Country = "Libya",
                TotalCapacity = 5000,
                IsActive = true,
                MapImageURL = "https://example.com/map-tripoli.png",
                CreatedAt = DateTime.UtcNow
            };
            context.Venues.Add(venue1);

            var venue2 = new Venue
            {
                TenantID = tenant2.TenantID,
                Name = "Dubai World Trade Centre",
                Address = "Sheikh Zayed Road",
                City = "Dubai",
                Country = "UAE",
                TotalCapacity = 25000,
                IsActive = true,
                MapImageURL = "https://example.com/map-dubai.png",
                CreatedAt = DateTime.UtcNow
            };
            context.Venues.Add(venue2);

            var venue3 = new Venue
            {
                TenantID = tenant2.TenantID,
                Name = "Riyadh Exhibition & Convention Center",
                Address = "King Abdullah Road",
                City = "Riyadh",
                Country = "Saudi Arabia",
                TotalCapacity = 15000,
                IsActive = true,
                MapImageURL = "https://example.com/map-riyadh.png",
                CreatedAt = DateTime.UtcNow
            };
            context.Venues.Add(venue3);
            await context.SaveChangesAsync();

            // Halls
            var hallA = new Hall
            {
                VenueID = venue1.VenueID,
                HallName = "Hall A",
                AreaSqM = 2000,
                MaxBooths = 40,
                FloorPlanWidth = 50,
                FloorPlanHeight = 40,
                FloorPlanJSON = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halls.Add(hallA);

            var hallB = new Hall
            {
                VenueID = venue1.VenueID,
                HallName = "Hall B",
                AreaSqM = 1500,
                MaxBooths = 30,
                FloorPlanWidth = 50,
                FloorPlanHeight = 30,
                FloorPlanJSON = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halls.Add(hallB);

            var zabeel1 = new Hall
            {
                VenueID = venue2.VenueID,
                HallName = "Zabeel Hall 1",
                AreaSqM = 5000,
                MaxBooths = 100,
                FloorPlanWidth = 100,
                FloorPlanHeight = 50,
                FloorPlanJSON = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halls.Add(zabeel1);

            var zabeel2 = new Hall
            {
                VenueID = venue2.VenueID,
                HallName = "Zabeel Hall 2",
                AreaSqM = 4000,
                MaxBooths = 80,
                FloorPlanWidth = 80,
                FloorPlanHeight = 50,
                FloorPlanJSON = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halls.Add(zabeel2);

            var saeed = new Hall
            {
                VenueID = venue2.VenueID,
                HallName = "Sheikh Saeed Hall",
                AreaSqM = 6000,
                MaxBooths = 120,
                FloorPlanWidth = 120,
                FloorPlanHeight = 50,
                FloorPlanJSON = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halls.Add(saeed);

            var ruh1 = new Hall
            {
                VenueID = venue3.VenueID,
                HallName = "Hall 1",
                AreaSqM = 3000,
                MaxBooths = 60,
                FloorPlanWidth = 60,
                FloorPlanHeight = 50,
                FloorPlanJSON = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halls.Add(ruh1);

            var ruh2 = new Hall
            {
                VenueID = venue3.VenueID,
                HallName = "Hall 2",
                AreaSqM = 3000,
                MaxBooths = 60,
                FloorPlanWidth = 60,
                FloorPlanHeight = 50,
                FloorPlanJSON = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Halls.Add(ruh2);
            await context.SaveChangesAsync();

            // 9. Seed Exhibitions
            var exhibition1 = new Exhibition
            {
                TenantID = tenant1.TenantID,
                VenueID = venue1.VenueID,
                Name = "Tripoli Tech Summit 2026",
                Type = "Technology",
                Edition = "1st",
                StartDate = DateTime.UtcNow.AddDays(-2).Date,
                EndDate = DateTime.UtcNow.AddDays(5).Date,
                Status = ExhibitionStatus.Open,
                ExpectedVisitors = 3000,
                EntryFee = 15.00m,
                EntryCurrency = "LYD",
                Description = "A premier event focusing on Digital Transformation and Tech Innovations in North Africa.",
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitions.Add(exhibition1);

            var exhibition2 = new Exhibition
            {
                TenantID = tenant1.TenantID,
                VenueID = venue1.VenueID,
                Name = "Libya International Book Fair",
                Type = "Cultural",
                Edition = "10th",
                StartDate = DateTime.UtcNow.AddMonths(1).Date,
                EndDate = DateTime.UtcNow.AddMonths(1).AddDays(10).Date,
                Status = ExhibitionStatus.Planning,
                ExpectedVisitors = 10000,
                EntryFee = 5.00m,
                EntryCurrency = "LYD",
                Description = "The annual international gathering for book publishers, writers, and cultural lectures.",
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitions.Add(exhibition2);

            var exhibition3 = new Exhibition
            {
                TenantID = tenant2.TenantID,
                VenueID = venue2.VenueID,
                Name = "Dubai AI & Cloud Expo 2026",
                Type = "Technology",
                Edition = "3rd",
                StartDate = DateTime.UtcNow.AddDays(-1).Date,
                EndDate = DateTime.UtcNow.AddDays(4).Date,
                Status = ExhibitionStatus.Open,
                ExpectedVisitors = 8000,
                EntryFee = 100.00m,
                EntryCurrency = "AED",
                Description = "The leading event for Artificial Intelligence, machine learning, and cloud infrastructure.",
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitions.Add(exhibition3);

            var exhibition4 = new Exhibition
            {
                TenantID = tenant2.TenantID,
                VenueID = venue3.VenueID,
                Name = "Riyadh Real Estate & Construction Fair",
                Type = "Real Estate",
                Edition = "5th",
                StartDate = DateTime.UtcNow.AddMonths(2).Date,
                EndDate = DateTime.UtcNow.AddMonths(2).AddDays(6).Date,
                Status = ExhibitionStatus.Planning,
                ExpectedVisitors = 12000,
                EntryFee = 150.00m,
                EntryCurrency = "SAR",
                Description = "Explore the future of infrastructure projects and smart housing in Riyadh.",
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitions.Add(exhibition4);

            var exhibition5 = new Exhibition
            {
                TenantID = tenant2.TenantID,
                VenueID = venue2.VenueID,
                Name = "Gulf Medical Devices Show 2025",
                Type = "Medical",
                Edition = "2nd",
                StartDate = DateTime.UtcNow.AddMonths(-3).Date,
                EndDate = DateTime.UtcNow.AddMonths(-3).AddDays(3).Date,
                Status = ExhibitionStatus.Closed,
                ExpectedVisitors = 5000,
                EntryFee = 50.00m,
                EntryCurrency = "USD",
                Description = "Medical machinery and healthcare services exhibition for Gulf hospitals.",
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitions.Add(exhibition5);
            await context.SaveChangesAsync();

            // 10. Exhibition Schedules
            var sched1 = new ExhibitionSchedule
            {
                ExhibitionID = exhibition3.ExhibitionID,
                HallID = zabeel1.HallID,
                EventName = "Cloud Native Architectures for Scale",
                Description = "Hands-on workshop teaching Kubernetes, Docker, and Microservices patterns.",
                StartDateTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
                EndDateTime = DateTime.UtcNow.AddDays(1).Date.AddHours(12),
                SpeakerName = "Speaker Name", EventType = EventType.Workshop
            };
            context.ExhibitionSchedules.Add(sched1);

            var sched2 = new ExhibitionSchedule
            {
                ExhibitionID = exhibition3.ExhibitionID,
                HallID = zabeel1.HallID,
                EventName = "Generative AI in Corporate Finance",
                Description = "A lecture explaining how LLMs and agentic AI are changing portfolio optimizations.",
                StartDateTime = DateTime.UtcNow.AddDays(2).Date.AddHours(14),
                EndDateTime = DateTime.UtcNow.AddDays(2).Date.AddHours(15),
                SpeakerName = "Speaker Name", EventType = EventType.Lecture
            };
            context.ExhibitionSchedules.Add(sched2);

            var sched3 = new ExhibitionSchedule
            {
                ExhibitionID = exhibition1.ExhibitionID,
                HallID = hallA.HallID,
                EventName = "Digital Transformation in North Africa",
                Description = "Keynote address examining telecom expansion and cloud infrastructure.",
                StartDateTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
                EndDateTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10).AddMinutes(30),
                SpeakerName = "Speaker Name", EventType = EventType.Lecture
            };
            context.ExhibitionSchedules.Add(sched3);
            await context.SaveChangesAsync();

            // 11. Exhibitors
            var exhibitor1 = new Exhibitor
            {
                TenantID = tenant1.TenantID,
                CompanyName = "LTT - Libya Telecom",
                ContactPerson = "Ali Mansour",
                Phone = "+218910000000",
                Email = "info@ltt.ly",
                Sector = "Telecommunications",
                Nationality = "Libyan",
                ExhibitorCategory = ExhibitorCategory.Local,
                CompanyProfile = "Libya's primary internet service provider, offering FTTH and business network packages.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor1);

            var exhibitor2 = new Exhibitor
            {
                TenantID = tenant1.TenantID,
                CompanyName = "Al-Madar Al-Jadid",
                ContactPerson = "Salem Salem",
                Phone = "+218920000000",
                Email = "contact@almadar.ly",
                Sector = "Telecommunications",
                Nationality = "Libyan",
                ExhibitorCategory = ExhibitorCategory.Local,
                CompanyProfile = "Mobile network services and 4G/5G mobile internet provider.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor2);

            var exhibitor3 = new Exhibitor
            {
                TenantID = tenant1.TenantID,
                CompanyName = "Oracle Libya",
                ContactPerson = "Mohamed Ben-Ali",
                Phone = "+218911111111",
                Email = "contact@oracle.com",
                Sector = "Technology",
                Nationality = "International",
                ExhibitorCategory = ExhibitorCategory.International,
                CompanyProfile = "Global cloud databases, enterprise resources planning system software.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor3);

            var exhibitor4 = new Exhibitor
            {
                TenantID = tenant2.TenantID,
                CompanyName = "Microsoft Gulf",
                ContactPerson = "John Doe",
                Phone = "+97140000000",
                Email = "gulf@microsoft.com",
                Sector = "Technology",
                Nationality = "International",
                ExhibitorCategory = ExhibitorCategory.International,
                CompanyProfile = "Cloud systems, Azure platform, Office suite, AI Copilot products.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                UserId = user3.Id, // Linking to user3 for exhibitor login testing
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor4);

            var exhibitor5 = new Exhibitor
            {
                TenantID = tenant2.TenantID,
                CompanyName = "AWS Middle East",
                ContactPerson = "Jane Smith",
                Phone = "+97141111111",
                Email = "aws-me@amazon.com",
                Sector = "Technology",
                Nationality = "International",
                ExhibitorCategory = ExhibitorCategory.International,
                CompanyProfile = "Amazon Web Services, global computing capacity, S3, EC2.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor5);

            var exhibitor6 = new Exhibitor
            {
                TenantID = tenant2.TenantID,
                CompanyName = "Saudi Aramco",
                ContactPerson = "Saud Al-Saud",
                Phone = "+966110000000",
                Email = "info@aramco.com",
                Sector = "Energy",
                Nationality = "Saudi",
                ExhibitorCategory = ExhibitorCategory.Government,
                CompanyProfile = "The world's largest oil and chemical energy production corporation.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor6);

            var exhibitor7 = new Exhibitor
            {
                TenantID = tenant2.TenantID,
                CompanyName = "Emaar Properties",
                ContactPerson = "Ahmad Al-Maktoum",
                Phone = "+97142222222",
                Email = "sales@emaar.ae",
                Sector = "Real Estate",
                Nationality = "Emirati",
                ExhibitorCategory = ExhibitorCategory.Local,
                CompanyProfile = "Leading real estate developers, builder of Burj Khalifa and Dubai Mall.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor7);

            var exhibitor8 = new Exhibitor
            {
                TenantID = tenant2.TenantID,
                CompanyName = "Riyadh Municipality",
                ContactPerson = "Khalid Al-Rashed",
                Phone = "+966112222222",
                Email = "contact@alriyadh.gov.sa",
                Sector = "Government",
                Nationality = "Saudi",
                ExhibitorCategory = ExhibitorCategory.Government,
                CompanyProfile = "Public works department of Riyadh City council.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor8);

            var exhibitor9 = new Exhibitor
            {
                TenantID = tenant2.TenantID,
                CompanyName = "Dubai Health Authority",
                ContactPerson = "Fatima Al-Falasi",
                Phone = "+97143333333",
                Email = "info@dha.gov.ae",
                Sector = "Healthcare",
                Nationality = "Emirati",
                ExhibitorCategory = ExhibitorCategory.Government,
                CompanyProfile = "Regulator of healthcare facilities and hospitals in the Emirate of Dubai.",
                LogoURL = "https://example.com/logo.png", IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Exhibitors.Add(exhibitor9);
            await context.SaveChangesAsync();

            // 12. Booth Price Rules
            var rule1 = new BoothPriceRule
            {
                TenantID = tenant1.TenantID,
                RuleName = "Local Tech Rule",
                PricePerSqM = 120.00m,
                CurrencyCode = "LYD",
                ExhibitorCategory = ExhibitorCategory.Local,
                ProductCategory = "General", ValidFrom = DateTime.UtcNow.AddMonths(-1).Date,
                Notes = "Default rental rate for domestic Libyan technology organizations.",
                CreatedAt = DateTime.UtcNow
            };
            context.BoothPriceRules.Add(rule1);

            var rule2 = new BoothPriceRule
            {
                TenantID = tenant1.TenantID,
                RuleName = "International Tech Rule",
                PricePerSqM = 250.00m,
                CurrencyCode = "USD",
                ExhibitorCategory = ExhibitorCategory.International,
                ProductCategory = "General", ValidFrom = DateTime.UtcNow.AddMonths(-1).Date,
                Notes = "Standard rates for non-Libyan tech exhibitors charged in USD.",
                CreatedAt = DateTime.UtcNow
            };
            context.BoothPriceRules.Add(rule2);

            var rule3 = new BoothPriceRule
            {
                TenantID = tenant2.TenantID,
                RuleName = "Gulf Premium Rule",
                PricePerSqM = 1500.00m,
                CurrencyCode = "AED",
                ExhibitorCategory = ExhibitorCategory.International,
                ProductCategory = "General", ValidFrom = DateTime.UtcNow.AddMonths(-2).Date,
                Notes = "Standard corporate rate in Dubai trade center for international exhibitors.",
                CreatedAt = DateTime.UtcNow
            };
            context.BoothPriceRules.Add(rule3);

            var rule4 = new BoothPriceRule
            {
                TenantID = tenant2.TenantID,
                RuleName = "Gulf Local Rule",
                PricePerSqM = 1000.00m,
                CurrencyCode = "AED",
                ExhibitorCategory = ExhibitorCategory.Local,
                ProductCategory = "General", ValidFrom = DateTime.UtcNow.AddMonths(-2).Date,
                Notes = "Reduced rates for local UAE businesses.",
                CreatedAt = DateTime.UtcNow
            };
            context.BoothPriceRules.Add(rule4);

            var rule5 = new BoothPriceRule
            {
                TenantID = tenant2.TenantID,
                RuleName = "Saudi Govt Rule",
                PricePerSqM = 500.00m,
                CurrencyCode = "SAR",
                ExhibitorCategory = ExhibitorCategory.Government,
                ProductCategory = "General", ValidFrom = DateTime.UtcNow.AddMonths(-2).Date,
                Notes = "Special subsidised pricing for official governmental departments.",
                CreatedAt = DateTime.UtcNow
            };
            context.BoothPriceRules.Add(rule5);
            await context.SaveChangesAsync();

            // 13. Booths
            // Hall A Booths
            var bTA01 = new Booth { HallID = hallA.HallID, BoothNumber = "T-A01", OriginalAreaSqM = 15m, CurrentAreaSqM = 15m, Status = BoothStatus.Reserved, PosX = 10, PosY = 10, Width = 3, Height = 5, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bTA02 = new Booth { HallID = hallA.HallID, BoothNumber = "T-A02", OriginalAreaSqM = 20m, CurrentAreaSqM = 20m, Status = BoothStatus.Reserved, PosX = 20, PosY = 10, Width = 4, Height = 5, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bTA03 = new Booth { HallID = hallA.HallID, BoothNumber = "T-A03", OriginalAreaSqM = 12m, CurrentAreaSqM = 12m, Status = BoothStatus.Available, PosX = 30, PosY = 10, Width = 3, Height = 4, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bTA04 = new Booth { HallID = hallA.HallID, BoothNumber = "T-A04", OriginalAreaSqM = 25m, CurrentAreaSqM = 25m, Status = BoothStatus.Available, PosX = 10, PosY = 25, Width = 5, Height = 5, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bTA05 = new Booth { HallID = hallA.HallID, BoothNumber = "T-A05", OriginalAreaSqM = 30m, CurrentAreaSqM = 30m, Status = BoothStatus.Maintenance, PosX = 25, PosY = 25, Width = 5, Height = 6, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            context.Booths.AddRange(bTA01, bTA02, bTA03, bTA04, bTA05);

            // Zabeel Hall 1 Booths
            var bDXB01 = new Booth { HallID = zabeel1.HallID, BoothNumber = "DXB-Z01", OriginalAreaSqM = 50m, CurrentAreaSqM = 50m, Status = BoothStatus.Reserved, PosX = 5, PosY = 5, Width = 5, Height = 10, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bDXB02 = new Booth { HallID = zabeel1.HallID, BoothNumber = "DXB-Z02", OriginalAreaSqM = 100m, CurrentAreaSqM = 100m, Status = BoothStatus.Reserved, PosX = 20, PosY = 5, Width = 10, Height = 10, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bDXB03 = new Booth { HallID = zabeel1.HallID, BoothNumber = "DXB-Z03", OriginalAreaSqM = 40m, CurrentAreaSqM = 40m, Status = BoothStatus.Available, PosX = 40, PosY = 5, Width = 8, Height = 5, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bDXB04 = new Booth { HallID = zabeel1.HallID, BoothNumber = "DXB-Z04", OriginalAreaSqM = 30m, CurrentAreaSqM = 30m, Status = BoothStatus.Available, PosX = 50, PosY = 5, Width = 6, Height = 5, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bDXB05 = new Booth { HallID = zabeel1.HallID, BoothNumber = "DXB-Z05", OriginalAreaSqM = 80m, CurrentAreaSqM = 80m, Status = BoothStatus.Reserved, PosX = 5, PosY = 25, Width = 8, Height = 10, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            context.Booths.AddRange(bDXB01, bDXB02, bDXB03, bDXB04, bDXB05);

            // Riyadh Hall 1 Booths
            var bRUH01 = new Booth { HallID = ruh1.HallID, BoothNumber = "RUH-H01", OriginalAreaSqM = 60m, CurrentAreaSqM = 60m, Status = BoothStatus.Reserved, PosX = 10, PosY = 10, Width = 6, Height = 10, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bRUH02 = new Booth { HallID = ruh1.HallID, BoothNumber = "RUH-H02", OriginalAreaSqM = 40m, CurrentAreaSqM = 40m, Status = BoothStatus.Reserved, PosX = 25, PosY = 10, Width = 5, Height = 8, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bRUH03 = new Booth { HallID = ruh1.HallID, BoothNumber = "RUH-H03", OriginalAreaSqM = 30m, CurrentAreaSqM = 30m, Status = BoothStatus.Available, PosX = 40, PosY = 10, Width = 5, Height = 6, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            var bRUH04 = new Booth { HallID = ruh1.HallID, BoothNumber = "RUH-H04", OriginalAreaSqM = 20m, CurrentAreaSqM = 20m, Status = BoothStatus.Available, PosX = 10, PosY = 30, Width = 4, Height = 5, ShapeType = BoothShapeType.Rect, ShapePolygonJSON = "[]", CreatedAt = DateTime.UtcNow };
            context.Booths.AddRange(bRUH01, bRUH02, bRUH03, bRUH04);
            await context.SaveChangesAsync();

            // 14. Seed Services
            // techorg services
            var sWifi1 = new Service { TenantID = tenant1.TenantID, ServiceName = "High-Speed WiFi", Category = "Internet", Unit = "Device", DefaultPrice = 50.00m, Description = "Fast internet connection for booth staff.", IsActive = true, CreatedAt = DateTime.UtcNow };
            var sPower1 = new Service { TenantID = tenant1.TenantID, ServiceName = "3-Phase Power Outlet", Category = "Electricity", Unit = "Connection", DefaultPrice = 150.00m, Description = "16A 3-Phase power supply for server demo booths.", IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Services.AddRange(sWifi1, sPower1);

            // gulfexpo services
            var sWifi2 = new Service { TenantID = tenant2.TenantID, ServiceName = "Premium Business WiFi", Category = "Internet", Unit = "Device", DefaultPrice = 250.00m, Description = "Corporate tier high-throughput internet.", IsActive = true, CreatedAt = DateTime.UtcNow };
            var sPower2 = new Service { TenantID = tenant2.TenantID, ServiceName = "3-Phase Power Socket", Category = "Electricity", Unit = "Connection", DefaultPrice = 600.00m, Description = "63A high-draw electricity connection.", IsActive = true, CreatedAt = DateTime.UtcNow };
            var sClean2 = new Service { TenantID = tenant2.TenantID, ServiceName = "Daily Booth Cleaning", Category = "Cleaning", Unit = "Day", DefaultPrice = 100.00m, Description = "Daily carpet vacuuming and trash pickup.", IsActive = true, CreatedAt = DateTime.UtcNow };
            var sCater2 = new Service { TenantID = tenant2.TenantID, ServiceName = "VIP Lunch Buffet", Category = "Catering", Unit = "Person", DefaultPrice = 150.00m, Description = "Fresh daily lunch tray provided for representatives.", IsActive = true, CreatedAt = DateTime.UtcNow };
            var sFurn2 = new Service { TenantID = tenant2.TenantID, ServiceName = "VIP Lounge Furniture Set", Category = "Furniture", Unit = "Set", DefaultPrice = 800.00m, Description = "Chic sofa, 2 chairs, a coffee table, and area rug.", IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Services.AddRange(sWifi2, sPower2, sClean2, sCater2, sFurn2);
            await context.SaveChangesAsync();

            // 15. Booth Reservations & Links
            // Reservation 1: Oracle Libya booking T-A01 (USD pricing) in Tripoli Tech Summit (Open)
            var res1 = new BoothReservation
            {
                ExhibitorID = exhibitor3.ExhibitorID,
                BoothID = bTA01.BoothID,
                ExhibitionID = exhibition1.ExhibitionID,
                BoothTypeSelected = BoothType.Premium,
                RequestedAreaSqM = 15m,
                AllocatedAreaSqM = 15m,
                ExhibitorCategory = ExhibitorCategory.International,
                BoothAmount = 15m * 250.00m, // 3750 USD
                ServicesAmount = 50.00m + 150.00m, // WiFi + Power = 200 USD
                TotalAmount = 3950.00m,
                CurrencyCode = "USD",
                ExchangeRateUsed = 1.0m,
                AmountInBaseCurrency = 3950.00m,
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTime.UtcNow.AddDays(-5),
                LogisticNotes = "Requires delivery of servers 1 day prior. Ensure power is live.",
                CreatedByUserId = user1.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.BoothReservations.Add(res1);

            // Reservation 2: LTT booking T-A02 (LYD pricing) in Tripoli Tech Summit (Open)
            var res2 = new BoothReservation
            {
                ExhibitorID = exhibitor1.ExhibitorID,
                BoothID = bTA02.BoothID,
                ExhibitionID = exhibition1.ExhibitionID,
                BoothTypeSelected = BoothType.Standard,
                RequestedAreaSqM = 20m,
                AllocatedAreaSqM = 20m,
                ExhibitorCategory = ExhibitorCategory.Local,
                BoothAmount = 20m * 120.00m, // 2400 LYD
                ServicesAmount = 0.00m,
                TotalAmount = 2400.00m,
                CurrencyCode = "LYD",
                ExchangeRateUsed = 0.2083m, // 1 LYD = 0.2083 USD
                AmountInBaseCurrency = 2400.00m * 0.2083m, // ~500 USD
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTime.UtcNow.AddDays(-4),
                LogisticNotes = "Simple banner backdrop setup. No special loading dock needs.",
                CreatedByUserId = user1.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.BoothReservations.Add(res2);

            // Reservation 3: Microsoft Gulf booking DXB-Z01 (AED pricing) in Dubai AI Expo (Open)
            var res3 = new BoothReservation
            {
                ExhibitorID = exhibitor4.ExhibitorID,
                BoothID = bDXB01.BoothID,
                ExhibitionID = exhibition3.ExhibitionID,
                BoothTypeSelected = BoothType.VIP,
                RequestedAreaSqM = 50m,
                AllocatedAreaSqM = 50m,
                ExhibitorCategory = ExhibitorCategory.International,
                BoothAmount = 50m * 1500.00m, // 75000 AED
                ServicesAmount = 800.00m + (10m * 150.00m), // Furniture + 10 Gourmet Buffet = 2300 AED
                TotalAmount = 77300.00m,
                CurrencyCode = "AED",
                ExchangeRateUsed = 1.0m, // Tenant base is AED
                AmountInBaseCurrency = 77300.00m,
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTime.UtcNow.AddDays(-10),
                LogisticNotes = "Need high ceiling space for hanging banners. Furniture layout custom.",
                CreatedByUserId = user2.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.BoothReservations.Add(res3);

            // Reservation 4: AWS Middle East booking DXB-Z02 (AED pricing) in Dubai AI Expo (Open)
            var res4 = new BoothReservation
            {
                ExhibitorID = exhibitor5.ExhibitorID,
                BoothID = bDXB02.BoothID,
                ExhibitionID = exhibition3.ExhibitionID,
                BoothTypeSelected = BoothType.VIP,
                RequestedAreaSqM = 100m,
                AllocatedAreaSqM = 100m,
                ExhibitorCategory = ExhibitorCategory.International,
                BoothAmount = 100m * 1500.00m, // 150000 AED
                ServicesAmount = 600.00m + 250.00m, // Power Socket + Business WiFi = 850 AED
                TotalAmount = 150850.00m,
                CurrencyCode = "AED",
                ExchangeRateUsed = 1.0m,
                AmountInBaseCurrency = 150850.00m,
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTime.UtcNow.AddDays(-9),
                LogisticNotes = "Power requirements critical. Will run active GPU hardware rigs.",
                CreatedByUserId = user2.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.BoothReservations.Add(res4);

            // Reservation 5: Saudi Aramco booking RUH-H01 (SAR pricing) in Riyadh Real Estate (Planning)
            var res5 = new BoothReservation
            {
                ExhibitorID = exhibitor6.ExhibitorID,
                BoothID = bRUH01.BoothID,
                ExhibitionID = exhibition4.ExhibitionID,
                BoothTypeSelected = BoothType.VIP,
                RequestedAreaSqM = 60m,
                AllocatedAreaSqM = 60m,
                ExhibitorCategory = ExhibitorCategory.Government,
                BoothAmount = 60m * 500.00m, // 30000 SAR
                ServicesAmount = 0m,
                TotalAmount = 30000.00m,
                CurrencyCode = "SAR",
                ExchangeRateUsed = 0.9790m, // 1 SAR = 0.979 AED
                AmountInBaseCurrency = 30000.00m * 0.9790m, // ~29370 AED
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTime.UtcNow.AddDays(-2),
                LogisticNotes = "Interactive holographic display unit will be shipped from Dhahran.",
                CreatedByUserId = user2.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.BoothReservations.Add(res5);

            // Reservation 6: Riyadh Municipality booking RUH-H02 (SAR pricing) in Riyadh Real Estate (Planning)
            var res6 = new BoothReservation
            {
                ExhibitorID = exhibitor8.ExhibitorID,
                BoothID = bRUH02.BoothID,
                ExhibitionID = exhibition4.ExhibitionID,
                BoothTypeSelected = BoothType.Corner,
                RequestedAreaSqM = 40m,
                AllocatedAreaSqM = 40m,
                ExhibitorCategory = ExhibitorCategory.Government,
                BoothAmount = 40m * 500.00m, // 20000 SAR
                ServicesAmount = 0m,
                TotalAmount = 20000.00m,
                CurrencyCode = "SAR",
                ExchangeRateUsed = 0.9790m,
                AmountInBaseCurrency = 20000.00m * 0.9790m, // ~19580 AED
                Status = ReservationStatus.Pending,
                ReservationDate = DateTime.UtcNow.AddDays(-1),
                LogisticNotes = "Waiting on final municipal budget approvals. Draft layout ready.",
                CreatedByUserId = user2.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.BoothReservations.Add(res6);

            await context.SaveChangesAsync();

            // 16. Reservation Services (Mapping link tables)
            context.ReservationServices.AddRange(
                new ReservationService { ReservationID = res1.ReservationID, ServiceID = sWifi1.ServiceID, Quantity = 1m, UnitPrice = 50.00m, CurrencyCode = "USD", TotalPrice = 50.00m },
                new ReservationService { ReservationID = res1.ReservationID, ServiceID = sPower1.ServiceID, Quantity = 1m, UnitPrice = 150.00m, CurrencyCode = "USD", TotalPrice = 150.00m },
                new ReservationService { ReservationID = res3.ReservationID, ServiceID = sFurn2.ServiceID, Quantity = 1m, UnitPrice = 800.00m, CurrencyCode = "AED", TotalPrice = 800.00m },
                new ReservationService { ReservationID = res3.ReservationID, ServiceID = sCater2.ServiceID, Quantity = 10m, UnitPrice = 150.00m, CurrencyCode = "AED", TotalPrice = 1500.00m },
                new ReservationService { ReservationID = res4.ReservationID, ServiceID = sPower2.ServiceID, Quantity = 1m, UnitPrice = 600.00m, CurrencyCode = "AED", TotalPrice = 600.00m },
                new ReservationService { ReservationID = res4.ReservationID, ServiceID = sWifi2.ServiceID, Quantity = 1m, UnitPrice = 250.00m, CurrencyCode = "AED", TotalPrice = 250.00m }
            );
            await context.SaveChangesAsync();

            // 17. Invoices & Invoice Items
            // Invoice 1 (Oracle) - USD, Paid
            var inv1 = new Invoice
            {
                TenantID = tenant1.TenantID,
                ReservationID = res1.ReservationID,
                InvoiceNumber = "INV-TECH-2026-001",
                InvoiceDate = DateTime.UtcNow.AddDays(-5),
                SubTotal = 3950.00m,
                TaxRate = 0.00m,
                TaxAmount = 0.00m,
                TotalAmount = 3950.00m,
                CurrencyCode = "USD",
                Status = InvoiceStatus.Paid,
                DueDate = DateTime.UtcNow.AddDays(25).Date,
                Notes = "Paid in full via corporate credit card.",
                CreatedAt = DateTime.UtcNow
            };
            context.Invoices.Add(inv1);

            // Invoice 2 (LTT) - LYD, Partially Paid
            var inv2 = new Invoice
            {
                TenantID = tenant1.TenantID,
                ReservationID = res2.ReservationID,
                InvoiceNumber = "INV-TECH-2026-002",
                InvoiceDate = DateTime.UtcNow.AddDays(-4),
                SubTotal = 2400.00m,
                TaxRate = 5.00m,
                TaxAmount = 120.00m,
                TotalAmount = 2520.00m,
                CurrencyCode = "LYD",
                Status = InvoiceStatus.PartiallyPaid,
                DueDate = DateTime.UtcNow.AddDays(20).Date,
                Notes = "Awaiting final bank clearance for second installment.",
                CreatedAt = DateTime.UtcNow
            };
            context.Invoices.Add(inv2);

            // Invoice 3 (Microsoft) - AED, Paid
            var inv3 = new Invoice
            {
                TenantID = tenant2.TenantID,
                ReservationID = res3.ReservationID,
                InvoiceNumber = "INV-GULF-2026-001",
                InvoiceDate = DateTime.UtcNow.AddDays(-10),
                SubTotal = 77300.00m,
                TaxRate = 5.00m,
                TaxAmount = 3865.00m,
                TotalAmount = 81165.00m,
                CurrencyCode = "AED",
                Status = InvoiceStatus.Paid,
                DueDate = DateTime.UtcNow.AddDays(15).Date,
                Notes = "VAT registered invoicing.",
                CreatedAt = DateTime.UtcNow
            };
            context.Invoices.Add(inv3);

            // Invoice 4 (AWS) - AED, Issued
            var inv4 = new Invoice
            {
                TenantID = tenant2.TenantID,
                ReservationID = res4.ReservationID,
                InvoiceNumber = "INV-GULF-2026-002",
                InvoiceDate = DateTime.UtcNow.AddDays(-9),
                SubTotal = 150850.00m,
                TaxRate = 5.00m,
                TaxAmount = 7542.50m,
                TotalAmount = 158392.50m,
                CurrencyCode = "AED",
                Status = InvoiceStatus.Issued,
                DueDate = DateTime.UtcNow.AddDays(21).Date,
                Notes = "Awaiting bank transfer approval cycle.",
                CreatedAt = DateTime.UtcNow
            };
            context.Invoices.Add(inv4);

            // Invoice 5 (Saudi Aramco) - SAR, Paid
            var inv5 = new Invoice
            {
                TenantID = tenant2.TenantID,
                ReservationID = res5.ReservationID,
                InvoiceNumber = "INV-GULF-2026-003",
                InvoiceDate = DateTime.UtcNow.AddDays(-2),
                SubTotal = 30000.00m,
                TaxRate = 15.00m,
                TaxAmount = 4500.00m,
                TotalAmount = 34500.00m,
                CurrencyCode = "SAR",
                Status = InvoiceStatus.Paid,
                DueDate = DateTime.UtcNow.AddDays(30).Date,
                Notes = "Government entity VAT rate applied.",
                CreatedAt = DateTime.UtcNow
            };
            context.Invoices.Add(inv5);

            // Invoice 6 (Riyadh Municipality) - SAR, Draft
            var inv6 = new Invoice
            {
                TenantID = tenant2.TenantID,
                ReservationID = res6.ReservationID,
                InvoiceNumber = "INV-GULF-2026-004",
                InvoiceDate = DateTime.UtcNow.AddDays(-1),
                SubTotal = 20000.00m,
                TaxRate = 15.00m,
                TaxAmount = 3000.00m,
                TotalAmount = 23000.00m,
                CurrencyCode = "SAR",
                Status = InvoiceStatus.Draft,
                DueDate = DateTime.UtcNow.AddDays(40).Date,
                Notes = "Draft format for municipal pre-review.",
                CreatedAt = DateTime.UtcNow
            };
            context.Invoices.Add(inv6);

            await context.SaveChangesAsync();

            // Invoice Items
            context.InvoiceItems.AddRange(
                // Invoice 1 (Oracle)
                new InvoiceItem { InvoiceID = inv1.InvoiceID, ItemName = "Booth T-A01 Area Rental (15 sqm)", Quantity = 15m, UnitPrice = 250.00m, TotalPrice = 3750.00m },
                new InvoiceItem { InvoiceID = inv1.InvoiceID, ItemName = "High-Speed WiFi Service", Quantity = 1m, UnitPrice = 50.00m, TotalPrice = 50.00m },
                new InvoiceItem { InvoiceID = inv1.InvoiceID, ItemName = "3-Phase Power Connection", Quantity = 1m, UnitPrice = 150.00m, TotalPrice = 150.00m },

                // Invoice 2 (LTT)
                new InvoiceItem { InvoiceID = inv2.InvoiceID, ItemName = "Booth T-A02 Area Rental (20 sqm)", Quantity = 20m, UnitPrice = 120.00m, TotalPrice = 2400.00m },

                // Invoice 3 (Microsoft)
                new InvoiceItem { InvoiceID = inv3.InvoiceID, ItemName = "Booth DXB-Z01 Area Rental (50 sqm)", Quantity = 50m, UnitPrice = 1500.00m, TotalPrice = 75000.00m },
                new InvoiceItem { InvoiceID = inv3.InvoiceID, ItemName = "VIP Lounge Furniture Set lease", Quantity = 1m, UnitPrice = 800.00m, TotalPrice = 800.00m },
                new InvoiceItem { InvoiceID = inv3.InvoiceID, ItemName = "Catering services (Lunch trays x10)", Quantity = 10m, UnitPrice = 150.00m, TotalPrice = 1500.00m },

                // Invoice 4 (AWS)
                new InvoiceItem { InvoiceID = inv4.InvoiceID, ItemName = "Booth DXB-Z02 Area Rental (100 sqm)", Quantity = 100m, UnitPrice = 1500.00m, TotalPrice = 150000.00m },
                new InvoiceItem { InvoiceID = inv4.InvoiceID, ItemName = "3-Phase Power Socket connection", Quantity = 1m, UnitPrice = 600.00m, TotalPrice = 600.00m },
                new InvoiceItem { InvoiceID = inv4.InvoiceID, ItemName = "Premium Business WiFi setup", Quantity = 1m, UnitPrice = 250.00m, TotalPrice = 250.00m },

                // Invoice 5 (Aramco)
                new InvoiceItem { InvoiceID = inv5.InvoiceID, ItemName = "Booth RUH-H01 Area Rental (60 sqm)", Quantity = 60m, UnitPrice = 500.00m, TotalPrice = 30000.00m },

                // Invoice 6 (Riyadh Municipality)
                new InvoiceItem { InvoiceID = inv6.InvoiceID, ItemName = "Booth RUH-H02 Area Rental (40 sqm)", Quantity = 40m, UnitPrice = 500.00m, TotalPrice = 20000.00m }
            );
            await context.SaveChangesAsync();

            // 18. Payments
            context.Payments.AddRange(
                // Oracle USD payment
                new Payment { InvoiceID = inv1.InvoiceID, PaymentDate = DateTime.UtcNow.AddDays(-5), Amount = 3950.00m, CurrencyCode = "USD", Method = PaymentMethod.Online, ReferenceNo = "PAY-ORCL-99", Status = PaymentStatus.Completed, Notes = "Online checkout portal integration.", ReceivedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },

                // LTT Part payment
                new Payment { InvoiceID = inv2.InvoiceID, PaymentDate = DateTime.UtcNow.AddDays(-4), Amount = 1200.00m, CurrencyCode = "LYD", Method = PaymentMethod.BankTransfer, ReferenceNo = "TR-LTT-50", Status = PaymentStatus.Completed, Notes = "First installment bank transfer.", ReceivedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },

                // Microsoft AED payment
                new Payment { InvoiceID = inv3.InvoiceID, PaymentDate = DateTime.UtcNow.AddDays(-9), Amount = 81165.00m, CurrencyCode = "AED", Method = PaymentMethod.Online, ReferenceNo = "PAY-MSFT-101", Status = PaymentStatus.Completed, Notes = "Automatic invoice clearing.", ReceivedByUserId = user2.Id, CreatedAt = DateTime.UtcNow },

                // Saudi Aramco SAR payment
                new Payment { InvoiceID = inv5.InvoiceID, PaymentDate = DateTime.UtcNow.AddDays(-1), Amount = 34500.00m, CurrencyCode = "SAR", Method = PaymentMethod.Cheque, ReferenceNo = "CHQ-ARAMCO-22", Status = PaymentStatus.Completed, Notes = "Cheque drawn on Saudi National Bank.", ReceivedByUserId = user2.Id, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            // 19. Visitors (20 Records)
            var visitors = new List<Visitor>();
            string[] nationalities = { "Libyan", "Emirati", "Saudi", "Egyptian", "British", "American", "Jordanian" };
            string[] domains = { "gmail.com", "outlook.com", "yahoo.com", "tech.ly", "gulf.ae", "mail.sa" };

            for (int i = 1; i <= 21; i++)
            {
                int tenantId = (i % 2 == 0) ? tenant2.TenantID : tenant1.TenantID;
                string nat = nationalities[i % nationalities.Length];
                string dom = domains[i % domains.Length];
                string name = $"Visitor User {i}";

                visitors.Add(new Visitor
                {
                    TenantID = tenantId,
                    FullName = name,
                    Phone = $"+9665500000{i:00}",
                    Email = $"visitor{i}@{dom}",
                    Nationality = nat,
                    VisitorType = (i % 5 == 0) ? "VIP" : ((i % 7 == 0) ? "Student" : "General"),
                    RegisteredAt = DateTime.UtcNow.AddDays(-15 + (i % 10)),
                    CreatedAt = DateTime.UtcNow
                });
            }
            context.Visitors.AddRange(visitors);
            await context.SaveChangesAsync();

            // 20. Tickets & Ticket Scans
            // Generate tickets for visitors for Tripoli Tech Summit and Dubai AI Expo
            var tickets = new List<Ticket>();
            int qrcodeCounter = 1001;

            foreach (var vis in visitors)
            {
                // Link visitor to appropriate exhibition based on tenant
                int exhibId = (vis.TenantID == tenant1.TenantID) ? exhibition1.ExhibitionID : exhibition3.ExhibitionID;
                string tCurrency = (vis.TenantID == tenant1.TenantID) ? "LYD" : "AED";
                decimal price = (vis.VisitorType == "VIP") ? 250m : ((vis.VisitorType == "Student") ? 10m : 50m);

                tickets.Add(new Ticket
                {
                    VisitorID = vis.VisitorID,
                    ExhibitionID = exhibId,
                    TicketType = vis.VisitorType,
                    Price = price,
                    CurrencyCode = tCurrency,
                    QRCode = $"TICKET-QR-{qrcodeCounter++}",
                    ValidDate = DateTime.UtcNow.Date.AddDays(qrcodeCounter % 3),
                    Status = (qrcodeCounter % 15 == 0) ? TicketStatus.Cancelled : TicketStatus.Active,
                    IssuedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedAt = DateTime.UtcNow
                });
            }
            context.Tickets.AddRange(tickets);
            await context.SaveChangesAsync();

            // Scans (Simulating entry checks at gates)
            var scans = new List<TicketScan>();
            int scanGateCounter = 1;
            foreach (var ticket in tickets.Where(t => t.Status == TicketStatus.Active).Take(12))
            {
                scans.Add(new TicketScan
                {
                    TicketID = ticket.TicketID,
                    ScanDateTime = DateTime.UtcNow.AddDays(-1).AddHours(9).AddMinutes(scanGateCounter * 7),
                    GateName = $"Main Gate {(scanGateCounter % 3) + 1}",
                    Direction = ScanDirection.In,
                    ScannedByUserId = user2.Id
                });
                // Some exit scans
                if (scanGateCounter % 3 == 0)
                {
                    scans.Add(new TicketScan
                    {
                        TicketID = ticket.TicketID,
                        ScanDateTime = DateTime.UtcNow.AddDays(-1).AddHours(17).AddMinutes(scanGateCounter * 12),
                        GateName = $"Exit Gate {(scanGateCounter % 2) + 1}",
                        Direction = ScanDirection.Out,
                        ScannedByUserId = user2.Id
                    });
                }
                scanGateCounter++;
            }
            context.TicketScans.AddRange(scans);
            await context.SaveChangesAsync();

            // 21. Visitor Ratings (Satisfying rating ranges 1 to 5)
            context.VisitorRatings.AddRange(
                new VisitorRating { VisitorID = visitors[0].VisitorID, ExhibitionID = exhibition1.ExhibitionID, ExhibitorID = exhibitor1.ExhibitorID, Score = 5, Comment = "Excellent network display and speeds!", RatedAt = DateTime.UtcNow.AddDays(-1) },
                new VisitorRating { VisitorID = visitors[1].VisitorID, ExhibitionID = exhibition3.ExhibitionID, ExhibitorID = exhibitor4.ExhibitorID, Score = 4, Comment = "Very nice booth setup, nice AI models demo.", RatedAt = DateTime.UtcNow.AddDays(-1) },
                new VisitorRating { VisitorID = visitors[2].VisitorID, ExhibitionID = exhibition3.ExhibitionID, ExhibitorID = exhibitor5.ExhibitorID, Score = 5, Comment = "AWS cloud compute workshops are super helpful.", RatedAt = DateTime.UtcNow.AddDays(-2) },
                new VisitorRating { VisitorID = visitors[4].VisitorID, ExhibitionID = exhibition1.ExhibitionID, Score = 4, Comment = "Well organised exhibition.", RatedAt = DateTime.UtcNow.AddDays(-1) }
            );
            await context.SaveChangesAsync();

            // 22. Exhibition Expenses (Operating overhead costs)
            context.Expenses.AddRange(
                new Expense { TenantID = tenant1.TenantID, ExhibitionID = exhibition1.ExhibitionID, Description = "Tripoli Hall A Lease Fee", Category = "Venue Rental", Amount = 15000.00m, CurrencyCode = "LYD", ExpenseDate = DateTime.UtcNow.AddDays(-20).Date, CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },
                new Expense { TenantID = tenant1.TenantID, ExhibitionID = exhibition1.ExhibitionID, Description = "Local Ads and Brochures printing", Category = "Marketing & Ads", Amount = 4000.00m, CurrencyCode = "LYD", ExpenseDate = DateTime.UtcNow.AddDays(-10).Date, CreatedByUserId = user1.Id, CreatedAt = DateTime.UtcNow },
                new Expense { TenantID = tenant2.TenantID, ExhibitionID = exhibition3.ExhibitionID, Description = "Dubai Trade Center Zabeel Hall Lease", Category = "Venue Rental", Amount = 60000.00m, CurrencyCode = "AED", ExpenseDate = DateTime.UtcNow.AddDays(-30).Date, CreatedByUserId = user2.Id, CreatedAt = DateTime.UtcNow },
                new Expense { TenantID = tenant2.TenantID, ExhibitionID = exhibition3.ExhibitionID, Description = "AI Keynote Speaker Hospitality", Category = "Catering & Speakers", Amount = 12000.00m, CurrencyCode = "AED", ExpenseDate = DateTime.UtcNow.AddDays(-15).Date, CreatedByUserId = user2.Id, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            // 23. Booth Staff (Exhibitor personnel)
            context.BoothStaffs.AddRange(
                new BoothStaff { ReservationID = res1.ReservationID, StaffName = "Hisham El-Gadi", Role = "Oracle Principal Engineer", Phone = "+218918887766", Email = "hisham@oracle.com", BadgeIssued = true, BadgeNumber = "BADGE-ORC-001" },
                new BoothStaff { ReservationID = res3.ReservationID, StaffName = "Sarah Connor", Role = "Azure Technical Lead", Phone = "+971509998877", Email = "sarah.connor@microsoft.com", BadgeIssued = true, BadgeNumber = "BADGE-MSF-001" },
                new BoothStaff { ReservationID = res3.ReservationID, StaffName = "T-800", Role = "AI Product Specialist", Phone = "+971509998878", Email = "t800@microsoft.com", BadgeIssued = false, BadgeNumber = "" }
            );
            await context.SaveChangesAsync();

            // 24. Products (Catalogues)
            context.Products.AddRange(
                new Product { ExhibitorID = exhibitor3.ExhibitorID, ExhibitionID = exhibition1.ExhibitionID, ProductName = "Oracle Autonomous Database Cloud", Category = "Software", Description = "Self-driving database services on Linux.", ImageURL = "https://example.com/oracle-auto-db.png" },
                new Product { ExhibitorID = exhibitor1.ExhibitorID, ExhibitionID = exhibition1.ExhibitionID, ProductName = "LTT Corporate FTTH", Category = "Fiber Optic", Description = "Enterprise dedicated fiber-optic internet connection with 99.9% uptime SLA.", ImageURL = "https://example.com/ltt-ftth.png" },
                new Product { ExhibitorID = exhibitor4.ExhibitorID, ExhibitionID = exhibition3.ExhibitionID, ProductName = "Azure AI Studio Integration", Category = "Cloud", Description = "Finetune custom LLM weights in a secure environment.", ImageURL = "https://example.com/azure-ai.png" }
            );
            await context.SaveChangesAsync();

            // 25. Audit Logs
            context.AuditLogs.AddRange(
                new AuditLog { TenantID = tenant1.TenantID, TableName = "BoothReservation", RecordID = res1.ReservationID.ToString(), Action = "Create", NewValues = "Reservation created for Oracle Libya on Booth T-A01.", OldValues = "", UserId = user1.Id, IPAddress = "127.0.0.1", ActionAt = DateTime.UtcNow.AddDays(-5) },
                new AuditLog { TenantID = tenant1.TenantID, TableName = "Invoice", RecordID = inv1.InvoiceID.ToString(), Action = "Update", NewValues = "Invoice INV-TECH-2026-001 updated to PAID.", OldValues = "", UserId = user1.Id, IPAddress = "127.0.0.1", ActionAt = DateTime.UtcNow.AddDays(-5) },
                new AuditLog { TenantID = tenant2.TenantID, TableName = "BoothReservation", RecordID = res3.ReservationID.ToString(), Action = "Create", NewValues = "Reservation created for Microsoft Gulf on Booth DXB-Z01.", OldValues = "", UserId = user2.Id, IPAddress = "127.0.0.1", ActionAt = DateTime.UtcNow.AddDays(-10) }
            );
            await context.SaveChangesAsync();
        }
    }
}



