using System;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class VisitorRatingDto
{
    public int RatingID { get; set; }
    public int VisitorID { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime RatedAt { get; set; }
}
