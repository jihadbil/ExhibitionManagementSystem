using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Product : IAuditableEntity, ISoftDeletable
{
    [Key] public int ProductID { get; set; }
    public int ExhibitorID { get; set; }
    public int ExhibitionID { get; set; }
    [Required, StringLength(200)] public string ProductName { get; set; }
    [StringLength(100)] public string Category { get; set; }
    public string Description { get; set; }
    [StringLength(500)] public string ImageURL { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(ExhibitorID))] public virtual Exhibitor Exhibitor { get; set; }
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

}
