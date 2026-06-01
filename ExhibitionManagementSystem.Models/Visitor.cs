using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Visitor : IAuditableEntity, ISoftDeletable
{
    [Key] public int VisitorID { get; set; }
    public int TenantID { get; set; }
    [Required, StringLength(100)] public string FullName { get; set; }
    [StringLength(20)] public string Phone { get; set; }
    [StringLength(200)] public string Email { get; set; }
    [StringLength(100)] public string Nationality { get; set; }
    [StringLength(50)] public string VisitorType { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }
    [StringLength(450)] public string? UserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(UserId))] public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
}
