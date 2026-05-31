using System;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class FinancialReportDto
{
    public int ReportID { get; set; }
    public int TenantID { get; set; }
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalVisitors { get; set; }
    public int TotalExhibitors { get; set; }
    public int TotalBooths { get; set; }
    public decimal OccupancyRate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedByUserId { get; set; } = string.Empty;
    public DateTime? ReportPeriodFrom { get; set; }
    public DateTime? ReportPeriodTo { get; set; }
}
