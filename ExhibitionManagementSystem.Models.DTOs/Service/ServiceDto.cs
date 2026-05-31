using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Service;

public class ServiceDto : AuditDto
{
    public int ServiceID { get; set; }
    public int TenantID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? DefaultPrice { get; set; }
    public bool IsMandatory { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
