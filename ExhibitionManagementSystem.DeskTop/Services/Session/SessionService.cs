using System;
using System.Collections.Generic;
using ExhibitionManagementSystem.Models.DTOs.Auth;

namespace ExhibitionManagementSystem.DeskTop.Services.Session;

/// <summary>
/// يحتفظ ببيانات المستخدم المسجل دخوله طوال عمر التطبيق
/// </summary>
public class SessionService
{
    public int TenantId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string TenantName { get; private set; } = string.Empty;
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTime TokenExpiresAt { get; private set; } = DateTime.MinValue;
    public IList<string> Roles { get; private set; } = [];

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    /// <summary>
    /// يتحقق إن كان المستخدم في دور "Admin"
    /// </summary>
    public bool IsAdmin => Roles.Contains("Admin");

    public bool IsTokenExpired => DateTime.UtcNow >= TokenExpiresAt;

    /// <summary>
    /// يُعيّن بيانات الجلسة بعد تسجيل الدخول الناجح
    /// </summary>
    public void SetSession(LoginResponseDto response)
    {
        TenantId = response.TenantID;
        UserId = response.UserId;
        FullName = response.FullName;
        Email = response.Email;
        TenantName = response.TenantName;
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        TokenExpiresAt = response.ExpiresAt;
        Roles = response.Roles ?? [];
    }

    /// <summary>
    /// تحديث توكنات الجلسة بعد التجديد الناجح
    /// </summary>
    public void UpdateTokens(string newAccessToken, string newRefreshToken, DateTime expiresAt)
    {
        AccessToken = newAccessToken;
        RefreshToken = newRefreshToken;
        TokenExpiresAt = expiresAt;
    }

    /// <summary>
    /// يمسح بيانات الجلسة عند تسجيل الخروج
    /// </summary>
    public void ClearSession()
    {
        TenantId = 0;
        UserId = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        TenantName = string.Empty;
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        TokenExpiresAt = DateTime.MinValue;
        Roles = [];
    }
}
