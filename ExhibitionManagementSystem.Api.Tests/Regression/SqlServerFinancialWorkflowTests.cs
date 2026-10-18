using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using ReservationHandler = ExhibitionManagementSystem.Services.Implementations.ReservationService;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION")))
            Skip = "Set EMS_SQL_TEST_CONNECTION to test against a disposable SQL Server database.";
    }
}

public sealed class SqlServerFinancialWorkflowTests
{
    [SqlServerFact]
    public async Task SqlServer_MigrationsPaymentsRefundsAndServiceDeletionRemainConsistent()
    {
        var databaseName = "EMS_Workflow_Test_" + Guid.NewGuid().ToString("N");
        Console.WriteLine($"Disposable test database: {databaseName}");
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION"))
        { InitialCatalog = databaseName, TrustServerCertificate = true };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
        try
        {
            await db.Database.MigrateAsync();
            var invoice = await WorkflowFixture.SeedAsync(db);
            using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
            var financial = new FinancialService(uow, WorkflowFixture.Mapper, new ConfigurationBuilder().Build(), db);
            foreach (var amount in new[] { 20m, 80m })
            {
                var payment = await financial.RecordPaymentAsync(invoice.TenantID, "workflow-user", new PaymentCreateDto
                { InvoiceID = invoice.InvoiceID, Amount = amount, CurrencyCode = "USD", Method = "Cash" });
                Assert.True(payment.IsSuccess, payment.ErrorMessage);
            }
            db.ChangeTracker.Clear();
            Assert.Equal(InvoiceStatus.Paid, (await db.Invoices.SingleAsync()).Status);
            Assert.Equal(500, await db.Payments.SumAsync(p => p.AmountInBaseCurrency));
            var refund = await financial.CreateCreditNoteAsync(invoice.TenantID, "workflow-user", invoice.InvoiceID,
                new CreditNoteCreateDto { Type = "Refund", Amount = 100, Reason = "Full refund test" });
            Assert.True(refund.IsSuccess, refund.ErrorMessage);
            var cancelled = await new ReservationHandler(uow, WorkflowFixture.Mapper, null!, null!)
                .CancelAsync(invoice.TenantID, invoice.ReservationID);
            Assert.True(cancelled.IsSuccess, cancelled.ErrorMessage);

            var draftReservation = new BoothReservation
            {
                TenantID = invoice.TenantID, ExhibitorID = invoice.Reservation.ExhibitorID,
                ExhibitionID = invoice.Reservation.ExhibitionID, BoothID = invoice.Reservation.BoothID,
                CurrencyCode = "USD", BaseCurrencyCode = "LYD", BoothSourceCurrencyCode = "USD",
                BoothAmount = 80, ServicesAmount = 20, TotalAmount = 100, ExchangeRateUsed = 5,
                AmountInBaseCurrency = 500, CreatedByUserId = "workflow-user"
            };
            var service = new Service { TenantID = invoice.TenantID, ServiceName = "Original service", DefaultCurrencyCode = "USD" };
            var reservationService = new ExhibitionManagementSystem.Models.ReservationService
            {
                Reservation = draftReservation, Service = service, CurrencyCode = "USD", SourceCurrencyCode = "USD",
                Quantity = 1, UnitPrice = 20, TotalPrice = 20
            };
            db.ReservationServices.Add(reservationService);
            await db.SaveChangesAsync();
            var draft = await financial.GenerateInvoiceForReservationAsync(invoice.TenantID, draftReservation.ReservationID);
            Assert.True(draft.IsSuccess, draft.ErrorMessage);
            service.ServiceName = "Renamed service";
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var removed = await new ReservationHandler(uow, WorkflowFixture.Mapper, null!, null!)
                .RemoveServiceFromReservationAsync(invoice.TenantID, draftReservation.ReservationID, reservationService.ReservationServiceID);
            Assert.True(removed.IsSuccess, removed.ErrorMessage);
            db.ChangeTracker.Clear();
            var finalInvoice = await db.Invoices.Include(i => i.InvoiceItems).SingleAsync(i => i.InvoiceID == draft.Data!.InvoiceID);
            Assert.Single(finalInvoice.InvoiceItems);
            Assert.Equal(80, finalInvoice.TotalAmount);
            Assert.Equal(400, finalInvoice.TotalAmountInBaseCurrency);
            Assert.False(await db.ReservationServices.AnyAsync());

            var convertedService = new Service { TenantID = invoice.TenantID, ServiceName = "Converted service", DefaultPrice = 1, DefaultCurrencyCode = "EUR" };
            db.Services.Add(convertedService);
            db.ExchangeRates.Add(new ExchangeRate { FromCurrency = "EUR", ToCurrency = "USD", Rate = .333333m,
                RateDate = DateTime.UtcNow.Date, Source = "Test", CreatedByUserId = "workflow-user" });
            await db.SaveChangesAsync();
            var reservationHandler = new ReservationHandler(uow, WorkflowFixture.Mapper, new PricingService(uow, WorkflowFixture.Mapper), financial);
            var added = await reservationHandler.AddServiceToReservationAsync(invoice.TenantID, draftReservation.ReservationID,
                new ReservationServiceCreateDto { ServiceID = convertedService.ServiceID, Quantity = 3 });
            Assert.True(added.IsSuccess, added.ErrorMessage);
            db.ChangeTracker.Clear();
            var convertedItem = await db.InvoiceItems.SingleAsync(i => i.ReservationServiceID == added.Data!.ReservationServiceID);
            Assert.Equal(.333333m, convertedItem.UnitPrice);
            Assert.Equal(1, convertedItem.TotalPrice);
            Assert.Equal(convertedItem.TotalPrice, InvoiceFinancialSummary.Round(convertedItem.UnitPrice * convertedItem.Quantity));

            var hall = await db.Halls.SingleAsync();
            var otherExhibition = new Exhibition { TenantID = invoice.TenantID, VenueID = hall.VenueID, Name = "Other expo", EntryCurrency = "LYD" };
            db.Exhibitions.Add(otherExhibition);
            var mergeBooths = new[] {
                new Booth { TenantID = invoice.TenantID, HallID = hall.HallID, BoothNumber = "B-2", OriginalAreaSqM = 10, CurrentAreaSqM = 4 },
                new Booth { TenantID = invoice.TenantID, HallID = hall.HallID, BoothNumber = "B-3", OriginalAreaSqM = 10, CurrentAreaSqM = 4 }
            };
            db.Booths.AddRange(mergeBooths);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var boothService = new BoothService(uow, WorkflowFixture.Mapper);
            var merge = await boothService.MergeBoothsAsync(invoice.TenantID, "workflow-user", new BoothMergeCreateDto
            { HallID = hall.HallID, ExhibitionID = draftReservation.ExhibitionID, BoothIDs = mergeBooths.Select(b => b.BoothID).ToList(), MergedBoothLabel = "B-2+B-3" });
            Assert.True(merge.IsSuccess, merge.ErrorMessage);
            Assert.Equal(8, merge.Data!.MergedAreaSqM);
            var availability = await boothService.GetAvailableAsync(invoice.TenantID, hall.HallID, otherExhibition.ExhibitionID);
            Assert.Contains(availability.Data!, b => b.BoothID == mergeBooths[0].BoothID);
            var unmerge = await boothService.UnmergeBoothsAsync(invoice.TenantID, merge.Data.MergeID);
            Assert.True(unmerge.IsSuccess, unmerge.ErrorMessage);
            Assert.False(await db.BoothMergeItems.AnyAsync());
        }
        finally
        {
            await using var cleanup = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
            // Only the explicitly generated disposable database can be removed.
            Assert.Equal(databaseName, new SqlConnectionStringBuilder(cleanup.Database.GetConnectionString()).InitialCatalog);
            Assert.StartsWith("EMS_Workflow_Test_", databaseName);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
