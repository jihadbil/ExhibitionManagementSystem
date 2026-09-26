namespace ExhibitionManagementSystem.Services.Financial;

public static class InvoiceBalanceCalculator
{
    public static InvoiceBalance Calculate(
        decimal invoiceTotal,
        decimal grossPaid,
        decimal totalCredits,
        decimal totalRefunded)
    {
        var adjustedTotal = invoiceTotal - totalCredits;
        var netPaid = grossPaid - totalRefunded;
        return new InvoiceBalance(
            adjustedTotal,
            netPaid,
            Math.Max(0, adjustedTotal - netPaid));
    }
}

public readonly record struct InvoiceBalance(
    decimal AdjustedTotal,
    decimal NetPaid,
    decimal Remaining);
