using System;

namespace ExhibitionManagementSystem.Models.DTOs.Common;

public abstract class AuditDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
