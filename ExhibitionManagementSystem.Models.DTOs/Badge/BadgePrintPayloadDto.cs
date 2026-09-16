using System;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models.DTOs.Badge;

/// <summary>
/// يمثل حمولة البيانات المجهزة لطباعة الشارة فورياً في مكتب الاستقبال أو كشك الخدمة الذاتية.
/// </summary>
public class BadgePrintPayloadDto
{
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public string ParticipantTypeTitle { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public string? VenueAndHallName { get; set; }
    public string? DatesText { get; set; }
    public string? BoothNumber { get; set; }
    public string HeaderBgColor { get; set; } = "#6366F1";
    public string HeaderTextColor { get; set; } = "#FFFFFF";
    public string AccentColor { get; set; } = "#4F46E5";
    public string? LogoUrl { get; set; }
    public string? FooterText { get; set; }
    public int WidthMm { get; set; } = 86;
    public int HeightMm { get; set; } = 120;
    public BadgeOrientation Orientation { get; set; } = BadgeOrientation.Vertical;
    public string? ThermalZplCommand { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
