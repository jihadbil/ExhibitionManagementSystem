using System;
using System.Collections.Generic;

namespace ExhibitionManagementSystem.Models.DTOs.Auth;

public class UserManagementDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int TenantID { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLogin { get; set; }
    public List<string> Roles { get; set; } = [];
}
