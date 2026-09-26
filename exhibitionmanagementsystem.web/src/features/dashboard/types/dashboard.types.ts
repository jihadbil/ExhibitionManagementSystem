export interface DashboardStatsDto {
  activeExhibitions: number;
  occupiedBooths: number;
  totalExhibitors: number;
  totalVisitors: number;
  totalRevenue: number;
}

export interface MonthlyRevenuePoint {
  month: string;
  revenue: number;
  paid: number;
  pending: number;
}

export interface ExhibitionTypeItem {
  type: string;
  nameArabic: string;
  count: number;
  color: string;
  gradient: string;
}

export interface UpcomingExhibitionItem {
  exhibitionID: number;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
  venueName?: string;
  city?: string;
  totalBooths: number;
  occupiedBooths: number;
  boothOccupancyPercentage: number;
  status: string;
}
