using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothMergeCreateDto
{
    public int HallID { get; set; }
    public int ExhibitionID { get; set; }

    [Required]
    [StringLength(200)]
    public string MergedBoothLabel { get; set; } = string.Empty;

    [Required]
    public List<int> BoothIDs { get; set; } = [];

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
