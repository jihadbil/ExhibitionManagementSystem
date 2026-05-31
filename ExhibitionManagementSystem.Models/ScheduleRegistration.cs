using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

public class ScheduleRegistration : IAuditableEntity
{

    [Key] public int RegID { get; set; }
    public int ScheduleID { get; set; }
    public int VisitorID { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public RegistrationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(ScheduleID))] public virtual ExhibitionSchedule Schedule { get; set; }
    [ForeignKey(nameof(VisitorID))] public virtual Visitor Visitor { get; set; }

}
