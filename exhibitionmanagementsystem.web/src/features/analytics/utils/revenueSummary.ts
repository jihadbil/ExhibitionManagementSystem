export interface RevenuePoint { month: string; revenue: number; paid: number; pending: number }

export const calculateRevenueTotals = (points: readonly RevenuePoint[]) => ({
  revenue: points.reduce((sum, point) => sum + Math.round(point.revenue * 100), 0) / 100,
  paid: points.reduce((sum, point) => sum + Math.round(point.paid * 100), 0) / 100,
  // Month-end balances are snapshots, not transactions that can be added.
  pending: points.at(-1)?.pending ?? 0,
});
