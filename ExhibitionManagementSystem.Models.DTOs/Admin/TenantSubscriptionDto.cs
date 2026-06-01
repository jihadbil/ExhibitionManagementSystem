using System;

namespace ExhibitionManagementSystem.Models.DTOs.Admin;

public class TenantSubscriptionDto
{
    public int SubscriptionID { get; set; }
    public int TenantID { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
}
