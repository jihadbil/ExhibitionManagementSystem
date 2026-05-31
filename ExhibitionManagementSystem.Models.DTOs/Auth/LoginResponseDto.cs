using System;
using System.Collections.Generic;

namespace ExhibitionManagementSystem.Models.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TenantID { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}
