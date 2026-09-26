import { api } from '../../../services/api';
import type { DashboardStatsDto, MonthlyRevenuePoint, ExhibitionTypeItem, UpcomingExhibitionItem } from '../types/dashboard.types';

export const dashboardService = {
  async getStats(): Promise<DashboardStatsDto> {
    const response = await api.get<DashboardStatsDto>('/api/dashboard/stats');
    return response.data;
  },

  async getMonthlyRevenue(): Promise<MonthlyRevenuePoint[]> {
    const response = await api.get<MonthlyRevenuePoint[]>('/api/dashboard/revenue-chart?period=6m');
    return response.data;
  },

  getTypeDistribution(exhibitions: UpcomingExhibitionItem[]): ExhibitionTypeItem[] {
    const colors = ['#3b82f6', '#10b981', '#f97316', '#ec4899', '#8b5cf6', '#14b8a6'];
    const counts = exhibitions.reduce<Record<string, number>>((result, exhibition) => {
      const type = exhibition.type || 'Other';
      result[type] = (result[type] ?? 0) + 1;
      return result;
    }, {});

    return Object.entries(counts).map(([type, count], index) => ({
      type,
      nameArabic: type,
      count,
      color: colors[index % colors.length],
      gradient: 'var(--primary-gradient)',
    }));
  },

  async getUpcomingExhibitions(): Promise<UpcomingExhibitionItem[]> {
    const response = await api.get<UpcomingExhibitionItem[]>('/api/exhibitions/upcoming?count=6');
    return response.data;
  }
};
