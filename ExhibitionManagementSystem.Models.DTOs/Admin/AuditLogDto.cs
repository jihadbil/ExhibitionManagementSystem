using System;

namespace ExhibitionManagementSystem.Models.DTOs.Admin;

public class AuditLogDto
{
    public long LogID { get; set; }
    public int TenantID { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string RecordID { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
    public DateTime ActionAt { get; set; }
    public string IPAddress { get; set; } = string.Empty;
}
