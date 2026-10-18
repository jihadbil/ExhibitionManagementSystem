using System.Globalization;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class MoneyPrecisionTests
{
    [Theory]
    [InlineData("0.015")] [InlineData("99.995")]
    public async Task CreditsRejectUnsupportedPrecisionBeforeStartingAWrite(string raw)
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new FinancialService(uow, WorkflowFixture.Mapper, new ConfigurationBuilder().Build(), db)
            .CreateCreditNoteAsync(invoice.TenantID, "workflow-user", invoice.InvoiceID,
                new CreditNoteCreateDto { Type = "Credit", Amount = decimal.Parse(raw, CultureInfo.InvariantCulture), Reason = "Synthetic credit" });
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDIT_NOTE_AMOUNT", result.ErrorCode);
        Assert.Empty(db.CreditNotes);
    }
}
