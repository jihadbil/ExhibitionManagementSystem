using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class BoothPriceRule : IAuditableEntity, ISoftDeletable
{
    [Key] public int RuleID { get; set; }
    public int TenantID { get; set; }
    public int? ExhibitionID { get; set; }
    public BoothType? BoothType { get; set; }
    public ExhibitorCategory? ExhibitorCategory { get; set; }
    [StringLength(100)] public string ProductCategory { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PricePerSqM { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? MinAreaSqM { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? MaxAreaSqM { get; set; }
    [Column(TypeName = "date")] public DateTime ValidFrom { get; set; }
    [Column(TypeName = "date")] public DateTime? ValidTo { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

}
