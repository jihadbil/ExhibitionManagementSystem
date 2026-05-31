using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class BoothMergeItem
{
    [Key] public int ItemID { get; set; }
    public int MergeID { get; set; }
    public int BoothID { get; set; }
    public int SequenceOrder { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal OriginalAreaSqM { get; set; }

    [ForeignKey(nameof(MergeID))] public virtual BoothMerge Merge { get; set; }
    [ForeignKey(nameof(BoothID))] public virtual Booth Booth { get; set; }



}
