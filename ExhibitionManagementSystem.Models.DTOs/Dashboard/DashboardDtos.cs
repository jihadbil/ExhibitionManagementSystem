using System;

namespace ExhibitionManagementSystem.Models.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int TotalExhibitions { get; set; }
        public int ActiveExhibitions { get; set; }
        public int TotalExhibitors { get; set; }
        public int TotalVisitors { get; set; }
        public double TotalRevenue { get; set; }
        public double PendingRevenue { get; set; }
        public int TotalBooths { get; set; }
        public int OccupiedBooths { get; set; }
        public double OccupancyRate { get; set; }
        public int PendingReservations { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public string CurrencySymbol { get; set; } = "$";
    }

    public class ActivityItemDto
    {
        public int ActivityId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class RevenueChartPointDto
    {
        public string Label { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty; // Alias for chart mapping
        public double Revenue { get; set; }
        public double Paid { get; set; }
        public double Pending { get; set; }
    }
}
