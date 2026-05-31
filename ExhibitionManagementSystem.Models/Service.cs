using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Service : IAuditableEntity, ISoftDeletable
{

    [Key] public int ServiceID { get; set; }
    public int TenantID { get; set; }
    [Required, StringLength(200)] public string ServiceName { get; set; }
    [StringLength(100)] public string Category { get; set; }
    [StringLength(50)] public string Unit { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? DefaultPrice { get; set; }
    public bool IsMandatory { get; set; } = false;
    [StringLength(500)] public string Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

}
