using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

public readonly record struct InvoiceFinancialSummary(
    decimal AdjustedTotal, decimal NetPaid, decimal Remaining,
    decimal AdjustedBaseTotal, decimal NetBasePaid, decimal BaseRemaining, bool IsOverdue)
{
    public static InvoiceFinancialSummary Calculate(Invoice invoice)
    {
        var credits = invoice.CreditNotes.Sum(n => n.Amount);
        var refunds = invoice.CreditNotes.Where(n => n.Type == CreditNoteType.Refund).Sum(n => n.Amount);
        var payments = invoice.Payments.Where(p => p.Status == PaymentStatus.Completed).ToList();
        var adjusted = Math.Max(0, invoice.TotalAmount - credits);
        var paid = Math.Max(0, payments.Sum(p => p.Amount) - refunds);
        var remaining = Math.Max(0, adjusted - paid);
        var adjustedBase = Math.Max(0, invoice.TotalAmountInBaseCurrency - Round(credits * invoice.ExchangeRateUsed));
        var paidBase = Math.Max(0, payments.Sum(p => p.AmountInBaseCurrency) - Round(refunds * invoice.ExchangeRateUsed));
        return new(adjusted, paid, remaining, adjustedBase, paidBase,
            Round(remaining * invoice.ExchangeRateUsed),
            remaining > 0 && invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid &&
            invoice.DueDate.HasValue && invoice.DueDate.Value.Date < DateTime.UtcNow.Date);
    }

    public static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
