using System.Collections.Generic;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class VisitorRatingSummaryDto
{
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = [];
}
