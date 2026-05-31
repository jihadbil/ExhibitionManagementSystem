using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibition;

public class ExhibitionScheduleCreateDto
{
    public int ExhibitionID { get; set; }
    public int? HallID { get; set; }

    [Required]
    [StringLength(200)]
    public string EventName { get; set; } = string.Empty;

    public string? EventType { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }

    [StringLength(200)]
    public string SpeakerName { get; set; } = string.Empty;

    public int? MaxAttendees { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}
