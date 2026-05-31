using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Venue : IAuditableEntity, ISoftDeletable
{

    [Key] public int VenueID { get; set; }
    public int TenantID { get; set; }
    [Required, StringLength(200)] public string Name { get; set; }
    [StringLength(500)] public string Address { get; set; }
    [StringLength(100)] public string City { get; set; }
    [StringLength(100)] public string Country { get; set; }
    public int? TotalCapacity { get; set; }
    [StringLength(500)] public string MapImageURL { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    public virtual ICollection<Hall> Halls { get; set; } = new HashSet<Hall>();

}
