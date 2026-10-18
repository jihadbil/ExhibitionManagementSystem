using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using ReservationHandler = ExhibitionManagementSystem.Services.Implementations.ReservationService;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class FinancialWorkflowTests
{
    [Theory]
    [InlineData(InvoiceStatus.Issued)]
    [InlineData(InvoiceStatus.PartiallyPaid)]
    public async Task IssuedInvoice_AcceptsPaymentWithoutChangingFinancialSnapshot(InvoiceStatus status)
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, status);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new FinancialService(uow, WorkflowFixture.Mapper, new ConfigurationBuilder().Build(), db);
        var result = await service.RecordPaymentAsync(invoice.TenantID, "workflow-user", new PaymentCreateDto
        {
            InvoiceID = invoice.InvoiceID, Amount = 20, CurrencyCode = "USD", Method = "Cash"
        });
        Assert.True(result.IsSuccess, result.ErrorMessage);
        db.ChangeTracker.Clear();
        var saved = await db.Invoices.SingleAsync();
        Assert.Equal(InvoiceStatus.PartiallyPaid, saved.Status);
        Assert.Equal(100, saved.TotalAmount);
        Assert.Equal(500, saved.TotalAmountInBaseCurrency);
        Assert.Equal(100, (await db.Payments.SingleAsync()).AmountInBaseCurrency);
    }

    [Fact]
    public async Task DraftInvoice_RejectsPayment()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, InvoiceStatus.Draft);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new FinancialService(uow, WorkflowFixture.Mapper, new ConfigurationBuilder().Build(), db);
        var result = await service.RecordPaymentAsync(invoice.TenantID, "workflow-user", new PaymentCreateDto
        { InvoiceID = invoice.InvoiceID, Amount = 20, CurrencyCode = "USD", Method = "Cash" });
        Assert.Equal("INVOICE_NOT_ISSUED", result.ErrorCode);
        Assert.Empty(db.Payments);
    }

    [Fact]
    public async Task DraftItemChange_UpdatesBaseAmountAndPreservesItemIdentity()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, InvoiceStatus.Draft);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new InvoiceItemService(uow, WorkflowFixture.Mapper);
        var result = await service.CreateItemAsync(invoice.TenantID, invoice.InvoiceID,
            new InvoiceItemCreateDto { ItemName = "Extra", Quantity = 1, UnitPrice = 20 });
        Assert.True(result.IsSuccess);
        Assert.Equal(600, (await db.Invoices.SingleAsync()).TotalAmountInBaseCurrency);
        var updated = await service.UpdateItemAsync(invoice.TenantID, invoice.InvoiceID, result.Data!.InvoiceItemID,
            new InvoiceItemUpdateDto { InvoiceItemID = 999, ItemName = "Extra updated", Quantity = 1, UnitPrice = 30 });
        Assert.True(updated.IsSuccess);
        Assert.Equal(result.Data.InvoiceItemID, updated.Data!.InvoiceItemID);
        Assert.Equal(650, (await db.Invoices.SingleAsync()).TotalAmountInBaseCurrency);
    }

    [Fact]
    public async Task RemoveRenamedService_DeletesLinkedInvoiceItemAndRecalculatesAmounts()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, InvoiceStatus.Draft);
        var reservation = await db.BoothReservations.SingleAsync();
        var service = new Service { TenantID = invoice.TenantID, ServiceName = "Renamed electricity" };
        var rs = new ExhibitionManagementSystem.Models.ReservationService
        { Reservation = reservation, Service = service, CurrencyCode = "USD", SourceCurrencyCode = "USD", Quantity = 1, UnitPrice = 20, TotalPrice = 20 };
        db.ReservationServices.Add(rs);
        reservation.ServicesAmount = 20;
        reservation.TotalAmount = 120;
        reservation.AmountInBaseCurrency = 600;
        var trackedInvoice = await db.Invoices.SingleAsync();
        trackedInvoice.SubTotal = trackedInvoice.TotalAmount = 120;
        trackedInvoice.TotalAmountInBaseCurrency = 600;
        db.InvoiceItems.Add(new InvoiceItem { InvoiceID = invoice.InvoiceID, ReservationService = rs, ItemName = "Old electricity name", Quantity = 1, UnitPrice = 20, TotalPrice = 20 });
        await db.SaveChangesAsync();
        var rsId = rs.ReservationServiceID;
        db.ChangeTracker.Clear();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new ReservationHandler(uow, WorkflowFixture.Mapper, null!, null!)
            .RemoveServiceFromReservationAsync(invoice.TenantID, invoice.ReservationID, rsId);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        db.ChangeTracker.Clear();
        Assert.Empty(db.ReservationServices);
        Assert.Single(db.InvoiceItems);
        Assert.Equal(100, (await db.BoothReservations.SingleAsync()).TotalAmount);
        Assert.Equal(500, (await db.Invoices.SingleAsync()).TotalAmountInBaseCurrency);
    }

    [Fact]
    public async Task CancelIssuedReservation_SucceedsButCancelledReservationCannotBeApproved()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new ReservationHandler(uow, WorkflowFixture.Mapper, null!, null!);
        var cancelled = await service.CancelAsync(invoice.TenantID, invoice.ReservationID);
        Assert.True(cancelled.IsSuccess, cancelled.ErrorMessage);
        Assert.Equal(InvoiceStatus.Cancelled, (await db.Invoices.SingleAsync()).Status);
        var approved = await service.ApproveAsync(invoice.TenantID, invoice.ReservationID);
        Assert.Equal("INVALID_RESERVATION_TRANSITION", approved.ErrorCode);
    }

    [Fact]
    public async Task IssuedInvoice_StillRejectsFinancialMutation()
    {
        using var db = WorkflowFixture.CreateDb();
        await WorkflowFixture.SeedAsync(db);
        var invoice = await db.Invoices.SingleAsync();
        invoice.TotalAmount = 99;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void Summary_SubtractsRefundsAndPreservesZeroAdjustedTotal()
    {
        var invoice = new Invoice { TotalAmount = 100, TotalAmountInBaseCurrency = 500, ExchangeRateUsed = 5, Status = InvoiceStatus.Paid };
        invoice.Payments.Add(new Payment { Amount = 100, AmountInBaseCurrency = 500, Status = PaymentStatus.Completed });
        invoice.CreditNotes.Add(new CreditNote { Amount = 20, Type = CreditNoteType.Refund });
        var summary = InvoiceFinancialSummary.Calculate(invoice);
        Assert.Equal(80, summary.NetPaid);
        Assert.Equal(400, summary.NetBasePaid);
        Assert.Equal(0, summary.Remaining);
        invoice.CreditNotes.Add(new CreditNote { Amount = 80, Type = CreditNoteType.Credit });
        var dto = WorkflowFixture.Mapper.Map<InvoiceDto>(invoice);
        Assert.Equal(0, dto.AdjustedTotalAmount);
        Assert.Equal(0, dto.AdjustedTotalAmountInBaseCurrency);
    }

    [Fact]
    public void FullyPaidInvoice_DoesNotShowAPhantomBalanceFromPaymentRounding()
    {
        var invoice = new Invoice { TotalAmount = .02m, TotalAmountInBaseCurrency = .01m, ExchangeRateUsed = .333333m, Status = InvoiceStatus.Paid };
        invoice.Payments.Add(new Payment { Amount = .01m, AmountInBaseCurrency = 0, Status = PaymentStatus.Completed });
        invoice.Payments.Add(new Payment { Amount = .01m, AmountInBaseCurrency = 0, Status = PaymentStatus.Completed });
        var summary = InvoiceFinancialSummary.Calculate(invoice);
        Assert.Equal(0, summary.Remaining);
        Assert.Equal(0, summary.BaseRemaining);
    }
}
