namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothMergeItemDto
{
    public int MergeItemID { get; set; }
    public int MergeID { get; set; }
    public int BoothID { get; set; }
    public string BoothNumber { get; set; } = string.Empty;
    public decimal AreaSqM { get; set; }
}
