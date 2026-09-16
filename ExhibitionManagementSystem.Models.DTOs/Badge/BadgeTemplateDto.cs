using System;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models.DTOs.Badge;

public class BadgeTemplateDto
{
    public int TemplateID { get; set; }
    public int TenantID { get; set; }
    public int? ExhibitionID { get; set; }
    public string? ExhibitionName { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public ParticipantType TargetParticipantType { get; set; }
    public string TargetParticipantTypeName => TargetParticipantType.ToString();
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
    public BadgeOrientation Orientation { get; set; }
    public string HeaderBgColor { get; set; } = "#6366F1";
    public string HeaderTextColor { get; set; } = "#FFFFFF";
    public string AccentColor { get; set; } = "#4F46E5";
    public string CategoryBadgeText { get; set; } = string.Empty;
    public bool ShowLogo { get; set; }
    public string? LogoUrl { get; set; }
    public bool ShowQRCode { get; set; }
    public bool ShowBarcode { get; set; }
    public bool ShowCompanyName { get; set; }
    public bool ShowJobTitle { get; set; }
    public bool ShowHallName { get; set; }
    public bool ShowDates { get; set; }
    public string? CustomFooterText { get; set; }
    public string? CustomZplTemplate { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BadgeTemplateCreateDto
{
    public int? ExhibitionID { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public ParticipantType TargetParticipantType { get; set; } = ParticipantType.Visitor;
    public int WidthMm { get; set; } = 86;
    public int HeightMm { get; set; } = 120;
    public BadgeOrientation Orientation { get; set; } = BadgeOrientation.Vertical;
    public string HeaderBgColor { get; set; } = "#6366F1";
    public string HeaderTextColor { get; set; } = "#FFFFFF";
    public string AccentColor { get; set; } = "#4F46E5";
    public string CategoryBadgeText { get; set; } = string.Empty;
    public bool ShowLogo { get; set; } = true;
    public string? LogoUrl { get; set; }
    public bool ShowQRCode { get; set; } = true;
    public bool ShowBarcode { get; set; } = false;
    public bool ShowCompanyName { get; set; } = true;
    public bool ShowJobTitle { get; set; } = true;
    public bool ShowHallName { get; set; } = true;
    public bool ShowDates { get; set; } = true;
    public string? CustomFooterText { get; set; }
    public string? CustomZplTemplate { get; set; }
    public bool IsDefault { get; set; } = false;
}

public class BadgeTemplateUpdateDto : BadgeTemplateCreateDto
{
}
