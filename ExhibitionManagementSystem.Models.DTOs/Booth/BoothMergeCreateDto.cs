using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothMergeCreateDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int HallID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int ExhibitionID { get; set; }

    [Required]
    [StringLength(200)]
    public string MergedBoothLabel { get; set; } = string.Empty;

    [Required]
    public List<int> BoothIDs { get; set; } = [];

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
