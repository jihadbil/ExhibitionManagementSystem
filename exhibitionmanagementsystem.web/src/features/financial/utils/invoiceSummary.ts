export interface InvoiceAmounts {
  status: string;
  baseCurrencyCode: string;
  adjustedTotalAmountInBaseCurrency: number;
  paidAmountInBaseCurrency: number;
  remainingAmountInBaseCurrency: number;
  isOverdue: boolean;
}

const cents = (amount: number) => Number.isFinite(amount) ? Math.round(amount * 100) : 0;

export const summarizeInvoiceTotals = (invoices: InvoiceAmounts[]) => {
  const groups = new Map<string, { currency: string; billed: number; paid: number; remaining: number }>();
  let overdueCount = 0;
  for (const invoice of invoices) {
    if (invoice.status === 'Draft' || invoice.status === 'Cancelled') continue;
    const currency = invoice.baseCurrencyCode;
    const group = groups.get(currency) ?? { currency, billed: 0, paid: 0, remaining: 0 };
    group.billed += cents(invoice.adjustedTotalAmountInBaseCurrency);
    group.paid += cents(invoice.paidAmountInBaseCurrency);
    group.remaining += cents(invoice.remainingAmountInBaseCurrency);
    groups.set(currency, group);
    if (invoice.isOverdue) overdueCount++;
  }
  return {
    totals: Array.from(groups.values(), group => ({ currency: group.currency,
      totalBilled: group.billed / 100, totalCollected: group.paid / 100, remainingBalance: group.remaining / 100 })),
    overdueCount,
  };
};
