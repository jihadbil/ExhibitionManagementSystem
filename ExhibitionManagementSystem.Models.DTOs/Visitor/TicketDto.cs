using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class TicketDto : AuditDto
{
    public int TicketID { get; set; }
    public int VisitorID { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;
    public DateTime? ValidDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public int ScansCount { get; set; }
}
