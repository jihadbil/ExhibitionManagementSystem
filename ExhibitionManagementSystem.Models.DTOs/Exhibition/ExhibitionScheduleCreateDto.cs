using ExhibitionManagementSystem.Models.DTOs.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibition;

public class ExhibitionScheduleCreateDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int ExhibitionID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? HallID { get; set; }

    [Required]
    [StringLength(200)]
    public string EventName { get; set; } = string.Empty;

    [DefinedEnum(typeof(ExhibitionManagementSystem.Models.Enums.EventType))]
    public string? EventType { get; set; }
    public DateTime StartDateTime { get; set; }
    [DateAfter(nameof(StartDateTime))]
    public DateTime EndDateTime { get; set; }

    [StringLength(200)]
    public string SpeakerName { get; set; } = string.Empty;

    public int? MaxAttendees { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}
