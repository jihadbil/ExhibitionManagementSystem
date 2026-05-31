using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Enums;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Booth : IAuditableEntity, ISoftDeletable
{

    [Key] public int BoothID { get; set; }
    public int HallID { get; set; }
    [Required, StringLength(20)] public string BoothNumber { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal OriginalAreaSqM { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal CurrentAreaSqM { get; set; }
    public BoothStatus Status { get; set; }
    public bool IsMerged { get; set; } = false;
    public int? MergeID { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? PosX { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? PosY { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? Width { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? Height { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? RotationAngle { get; set; } = 0;
    public BoothShapeType? ShapeType { get; set; } = BoothShapeType.Rect;
    public string ShapePolygonJSON { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(HallID))] public virtual Hall Hall { get; set; }
    [ForeignKey(nameof(MergeID))] public virtual BoothMerge BoothMerge { get; set; }

}
