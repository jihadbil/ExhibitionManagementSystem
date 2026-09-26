using ExhibitionManagementSystem.Services.Financial;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Financial;

public sealed class InvoiceBalanceCalculatorTests
{
    [Fact]
    public void CreditNote_ReducesAmountDue()
    {
        var balance = InvoiceBalanceCalculator.Calculate(
            invoiceTotal: 100,
            grossPaid: 30,
            totalCredits: 20,
            totalRefunded: 0);

        Assert.Equal(80, balance.AdjustedTotal);
        Assert.Equal(30, balance.NetPaid);
        Assert.Equal(50, balance.Remaining);
    }

    [Fact]
    public void Refund_ReducesBothInvoiceValueAndNetPaid()
    {
        var balance = InvoiceBalanceCalculator.Calculate(
            invoiceTotal: 100,
            grossPaid: 100,
            totalCredits: 20,
            totalRefunded: 20);

        Assert.Equal(80, balance.AdjustedTotal);
        Assert.Equal(80, balance.NetPaid);
        Assert.Equal(0, balance.Remaining);
    }
}
