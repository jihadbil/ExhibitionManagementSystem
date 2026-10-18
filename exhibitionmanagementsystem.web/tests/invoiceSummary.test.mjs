import test from 'node:test';
import assert from 'node:assert/strict';
import { summarizeInvoiceTotals } from '../src/features/financial/utils/invoiceSummary.ts';

const invoice = (currency, billed, paid, remaining, status = 'Issued', isOverdue = false) => ({
  status, baseCurrencyCode: currency, adjustedTotalAmountInBaseCurrency: billed,
  paidAmountInBaseCurrency: paid, remainingAmountInBaseCurrency: remaining, isOverdue,
});

test('uses base snapshots and includes partial collections without mixing base currencies', () => {
  const result = summarizeInvoiceTotals([
    invoice('LYD', 500, 100, 400, 'PartiallyPaid'),
    invoice('LYD', 100, 100, 0, 'Paid'),
    invoice('EUR', 200, 50, 150),
  ]);
  assert.deepEqual(result.totals, [
    { currency: 'LYD', totalBilled: 600, totalCollected: 200, remainingBalance: 400 },
    { currency: 'EUR', totalBilled: 200, totalCollected: 50, remainingBalance: 150 },
  ]);
});

test('preserves zero after full credit and excludes drafts and cancellations', () => {
  const result = summarizeInvoiceTotals([
    invoice('LYD', 0, 0, 0, 'Paid'),
    invoice('LYD', 500, 0, 500, 'Draft'),
    invoice('LYD', 500, 0, 500, 'Cancelled'),
    invoice('LYD', 80, 20, 60, 'PartiallyPaid', true),
  ]);
  assert.equal(result.totals[0].totalBilled, 80);
  assert.equal(result.totals[0].totalCollected, 20);
  assert.equal(result.totals[0].remainingBalance, 60);
  assert.equal(result.overdueCount, 1);
});

test('adds money using integer cents', () => {
  const result = summarizeInvoiceTotals([invoice('LYD', 0.1, 0, 0.1), invoice('LYD', 0.2, 0, 0.2)]);
  assert.equal(result.totals[0].totalBilled, 0.3);
});
