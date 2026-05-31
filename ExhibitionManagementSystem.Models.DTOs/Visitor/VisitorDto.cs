using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class VisitorDto : AuditDto
{
    public int VisitorID { get; set; }
    public int TenantID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string VisitorType { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string? UserId { get; set; }
    public int TicketsCount { get; set; }
}
