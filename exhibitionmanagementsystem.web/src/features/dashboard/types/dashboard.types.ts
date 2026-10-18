export interface DashboardStatsDto {
  totalExhibitions: number;
  activeExhibitions: number;
  occupiedBooths: number;
  totalExhibitors: number;
  totalVisitors: number;
  totalRevenue: number | null;
  pendingRevenue: number | null;
  canViewFinancials?: boolean;
  totalBooths: number;
  occupancyRate: number;
  pendingReservations: number;
  currencyCode: string;
  currencySymbol: string;
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
  status: string;
}
