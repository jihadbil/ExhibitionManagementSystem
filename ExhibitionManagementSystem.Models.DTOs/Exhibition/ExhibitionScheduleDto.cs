using System;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibition;

public class ExhibitionScheduleDto
{
    public int ScheduleID { get; set; }
    public int ExhibitionID { get; set; }
    public int? HallID { get; set; }
    public string HallName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? EventType { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string SpeakerName { get; set; } = string.Empty;
    public int? MaxAttendees { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}
