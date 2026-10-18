import test from 'node:test';
import assert from 'node:assert/strict';
import { calculateRevenueTotals } from '../src/features/analytics/utils/revenueSummary.ts';

test('adds monthly movements but does not add repeated outstanding snapshots', () => {
  const points = Array.from({ length: 12 }, (_, i) => ({ month: `${i}`, revenue: i === 0 ? 100 : 0, paid: 0, pending: 100 }));
  assert.deepEqual(calculateRevenueTotals(points), { revenue: 100, paid: 0, pending: 100 });
});
test('uses last balance after collection and sums money in cents', () => {
  assert.deepEqual(calculateRevenueTotals([
    { month: '1', revenue: 0.1, paid: 0.1, pending: 80 },
    { month: '2', revenue: 0.2, paid: 0.2, pending: 0 },
  ]), { revenue: 0.3, paid: 0.3, pending: 0 });
});
test('empty chart has zero totals', () => {
  assert.deepEqual(calculateRevenueTotals([]), { revenue: 0, paid: 0, pending: 0 });
});
