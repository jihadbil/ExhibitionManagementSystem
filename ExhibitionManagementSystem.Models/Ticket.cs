using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

public class Ticket : IAuditableEntity, ISoftDeletable
{
    [Key] public int TicketID { get; set; }
    public int VisitorID { get; set; }
    public int ExhibitionID { get; set; }
    [Required, StringLength(50)] public string TicketType { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; } = 0;
    [StringLength(3)] public string? CurrencyCode { get; set; }
    [Required, StringLength(500)] public string QRCode { get; set; }
    [Column(TypeName = "date")] public DateTime? ValidDate { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public DateTime CreatedAt { get => IssuedAt; set => IssuedAt = value; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(VisitorID))] public virtual Visitor Visitor { get; set; }
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
    public virtual ICollection<TicketScan> TicketScans { get; set; } = new HashSet<TicketScan>();
}
