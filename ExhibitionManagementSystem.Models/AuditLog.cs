using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models;

public class AuditLog
{

    [Key] public long LogID { get; set; }
    public int TenantID { get; set; }
    [StringLength(450)] public string UserId { get; set; }
    [Required, StringLength(100)] public string TableName { get; set; }
    [Required, StringLength(100)] public string RecordID { get; set; }
    [Required, StringLength(20)] public string Action { get; set; }
    public string OldValues { get; set; }
    public string NewValues { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
    [StringLength(50)] public string IPAddress { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(UserId))] public virtual ApplicationUser User { get; set; }

}
