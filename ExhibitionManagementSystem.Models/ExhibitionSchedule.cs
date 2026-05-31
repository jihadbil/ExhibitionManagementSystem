using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

public class ExhibitionSchedule
{


    [Key] public int ScheduleID { get; set; }
    public int ExhibitionID { get; set; }
    public int? HallID { get; set; }
    [Required, StringLength(200)] public string EventName { get; set; }
    public EventType? EventType { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    [StringLength(200)] public string SpeakerName { get; set; }
    public int? MaxAttendees { get; set; }
    public string Description { get; set; }
    public bool IsPublic { get; set; } = true;

    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(HallID))] public virtual Hall Hall { get; set; }

}
