using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class BoothReservation : IAuditableEntity, ISoftDeletable
{

    [Key] public int ReservationID { get; set; }
    public int ExhibitorID { get; set; }
    public int? BoothID { get; set; }
    public int ExhibitionID { get; set; }
    public int? MergeID { get; set; }
    public BoothType BoothTypeSelected { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal RequestedAreaSqM { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal AllocatedAreaSqM { get; set; }
    public ExhibitorCategory ExhibitorCategory { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BoothAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ServicesAmount { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    [Column(TypeName = "decimal(18,6)")] public decimal ExchangeRateUsed { get; set; } = 1;
    [Column(TypeName = "decimal(18,2)")] public decimal AmountInBaseCurrency { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
    public string LogisticNotes { get; set; }
    [StringLength(450)] public string CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(ExhibitorID))] public virtual Exhibitor Exhibitor { get; set; }
    [ForeignKey(nameof(BoothID))] public virtual Booth Booth { get; set; }
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(MergeID))] public virtual BoothMerge BoothMerge { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
    [ForeignKey(nameof(CreatedByUserId))] public virtual ApplicationUser CreatedByUser { get; set; }
    public virtual Invoice Invoice { get; set; }
    public virtual ICollection<ReservationService> ReservationServices { get; set; } = new HashSet<ReservationService>();
}
