using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Tenant : IAuditableEntity
{
    [Key] public int TenantID { get; set; }
    [Required, StringLength(200)] public string CompanyName { get; set; }
    [StringLength(100)] public string Subdomain { get; set; }
    [Required, StringLength(50)] public string Plan { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? TrialEndsAt { get; set; }
    [StringLength(3)] public string BaseCurrency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(BaseCurrency))]
    public virtual Currency Currency { get; set; }
    public virtual ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new HashSet<TenantSubscription>();
}
