using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Exhibitor : IAuditableEntity, ISoftDeletable
{
    [Key] public int ExhibitorID { get; set; }
    public int TenantID { get; set; }
    [Required, StringLength(200)] public string CompanyName { get; set; }
    [StringLength(100)] public string ContactPerson { get; set; }
    [StringLength(20)] public string Phone { get; set; }
    [StringLength(200)] public string Email { get; set; }
    [StringLength(100)] public string Sector { get; set; }
    [StringLength(100)] public string Nationality { get; set; }
    public ExhibitorCategory ExhibitorCategory { get; set; }
    [StringLength(500)] public string LogoURL { get; set; }
    public string CompanyProfile { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }
    [StringLength(450)] public string? UserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(UserId))] public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<BoothReservation> BoothReservations { get; set; } = new HashSet<BoothReservation>();
}
