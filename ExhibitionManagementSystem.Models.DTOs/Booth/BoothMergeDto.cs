using System;
using System.Collections.Generic;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothMergeDto
{
    public int MergeID { get; set; }
    public int HallID { get; set; }
    public string HallName { get; set; } = string.Empty;
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public decimal MergedAreaSqM { get; set; }
    public DateTime MergeDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<BoothMergeItemDto> BoothItems { get; set; } = [];
}
