using ExhibitionManagementSystem.Controllers.Dashboard;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Dashboard;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class ReportingAndTenantWorkflowTests
{
    [Fact]
    public async Task Dashboard_UsesNetRefundedPaymentsAndReservationOccupancy()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, InvoiceStatus.Paid);
        var tracked = await db.Invoices.SingleAsync();
        tracked.Payments.Add(new Payment { TenantID = invoice.TenantID, CurrencyCode = "USD", BaseCurrencyCode = "LYD", Amount = 100, AmountInBaseCurrency = 500, ReceivedByUserId = "workflow-user" });
        tracked.CreditNotes.Add(new CreditNote { TenantID = invoice.TenantID, Amount = 20, CurrencyCode = "USD", Type = CreditNoteType.Refund, CreatedByUserId = "workflow-user", CreditNoteNumber = "CN-1", Reason = "Refund" });
        await db.SaveChangesAsync();
        var controller = new DashboardController(db) { ControllerContext = new ControllerContext { HttpContext = WorkflowFixture.Actor(invoice.TenantID).HttpContext! } };
        ((System.Security.Claims.ClaimsIdentity)controller.User.Identity!).AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Finance"));
        var response = await controller.GetStats();
        var stats = Assert.IsType<DashboardStatsDto>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(400, stats.TotalRevenue);
        Assert.Equal(1, stats.OccupiedBooths);
        Assert.Equal(100, stats.OccupancyRate);
    }

    [Fact]
    public async Task RevenueChart_UsesPaymentDateEvenForOldInvoices()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, InvoiceStatus.PartiallyPaid, DateTime.UtcNow.AddYears(-1));
        var tracked = await db.Invoices.SingleAsync();
        tracked.Payments.Add(new Payment { TenantID = invoice.TenantID, CurrencyCode = "USD", BaseCurrencyCode = "LYD", Amount = 20, AmountInBaseCurrency = 100, PaymentDate = DateTime.UtcNow, ReceivedByUserId = "workflow-user" });
        await db.SaveChangesAsync();
        var controller = new DashboardController(db) { ControllerContext = new ControllerContext { HttpContext = WorkflowFixture.Actor(invoice.TenantID).HttpContext! } };
        var response = await controller.GetRevenueChart();
        var points = Assert.IsAssignableFrom<IList<RevenueChartPointDto>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(100, points.Last().Paid);
        Assert.Equal(400, points.Last().Pending);
    }

    [Fact]
    public async Task Report_RejectsUnconvertedExpenseThenUsesHistoricalRate()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        db.Expenses.Add(new Expense { TenantID = invoice.TenantID, ExhibitionID = invoice.Reservation.ExhibitionID,
            Description = "Historical expense", Category = "Services", Amount = 10, CurrencyCode = "USD", BaseCurrencyCode = "LYD",
            ExchangeRateUsed = 0, ExpenseDate = DateTime.UtcNow.Date.AddDays(-3) });
        await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new ReportService(uow, WorkflowFixture.Mapper);
        var missing = await service.GenerateExhibitionReportAsync(invoice.TenantID, invoice.Reservation.ExhibitionID, "workflow-user");
        Assert.Equal("UNCONVERTED_EXPENSE", missing.ErrorCode);
        db.ExchangeRates.Add(new ExchangeRate { FromCurrency = "USD", ToCurrency = "LYD", Rate = 5,
            RateDate = DateTime.UtcNow.Date.AddDays(-4), Source = "Historical", CreatedByUserId = "workflow-user" });
        await db.SaveChangesAsync();
        // Report mapping is registered independently from the financial summary profile.
        var result = await service.GenerateExhibitionReportAsync(invoice.TenantID, invoice.Reservation.ExhibitionID, "workflow-user");
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(50, result.Data!.TotalExpenses);
        Assert.Equal(450, result.Data.NetProfit);
    }

    [Fact]
    public async Task Tenant_CannotChangeBaseCurrencyAfterFinancialActivity()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new TenantService(uow, WorkflowFixture.Mapper, db, null!, null!, null!,
            Options.Create(new TenantInvitationOptions()), NullLogger<TenantService>.Instance);
        var result = await service.UpdateAsync(invoice.TenantID, new TenantUpdateDto
        { CompanyName = "Tenant", Plan = "Trial", BaseCurrency = "USD", IsActive = true });
        Assert.Equal("BASE_CURRENCY_LOCKED", result.ErrorCode);
        Assert.Equal("LYD", (await db.Tenants.SingleAsync()).BaseCurrency);
    }
}
