using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل قالب تصميم وتخصيص بطاقات الدخول (Badges) للمشاركين.
/// </summary>
public class BadgeTemplate : IAuditableEntity, ISoftDeletable
{
    [Key]
    public int TemplateID { get; set; }

    public int TenantID { get; set; }

    /// <summary>
    /// المعرض المرتبط بالقالب (إن وجد، أو فارغ ليكون قالباً عاماً على مستوى المستأجر).
    /// </summary>
    public int? ExhibitionID { get; set; }

    [Required, StringLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// نوع المشارك الموجه له هذا القالب (عارض، زائر، VIP، متحدث، صحفي).
    /// </summary>
    public ParticipantType TargetParticipantType { get; set; }

    /// <summary>
    /// عرض البطاقة بالمليمتر (افتراضياً 86 مم لبطاقة CR80 القياسية أو 100 مم للشارات الكبيرة).
    /// </summary>
    public int WidthMm { get; set; } = 86;

    /// <summary>
    /// ارتفاع البطاقة بالمليمتر (افتراضياً 120 مم).
    /// </summary>
    public int HeightMm { get; set; } = 120;

    /// <summary>
    /// اتجاه الشارة (رأسي أو أفقي).
    /// </summary>
    public BadgeOrientation Orientation { get; set; } = BadgeOrientation.Vertical;

    /// <summary>
    /// لون خلفية الترويسة العلوية (HEX Code مثل #6366F1).
    /// </summary>
    [Required, StringLength(20)]
    public string HeaderBgColor { get; set; } = "#6366F1";

    /// <summary>
    /// لون نص الترويسة العلوية.
    /// </summary>
    [Required, StringLength(20)]
    public string HeaderTextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// لون التمييز للشريط السفلي أو شريط الفئة.
    /// </summary>
    [Required, StringLength(20)]
    public string AccentColor { get; set; } = "#4F46E5";

    /// <summary>
    /// النص المعروض في شريط الفئة (مثلاً: زائر / VISITOR).
    /// </summary>
    [StringLength(50)]
    public string CategoryBadgeText { get; set; } = string.Empty;

    public bool ShowLogo { get; set; } = true;

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    public bool ShowQRCode { get; set; } = true;

    public bool ShowBarcode { get; set; } = false;

    public bool ShowCompanyName { get; set; } = true;

    public bool ShowJobTitle { get; set; } = true;

    public bool ShowHallName { get; set; } = true;

    public bool ShowDates { get; set; } = true;

    [StringLength(200)]
    public string? CustomFooterText { get; set; }

    /// <summary>
    /// كود أوامر الطابعة الحرارية (ZPL) المخصص لطابعات Zebra/Dymo إن وجد.
    /// </summary>
    public string? CustomZplTemplate { get; set; }

    /// <summary>
    /// يحدد ما إذا كان هذا القالب هو الافتراضي لفئة المشارك المحددة.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(TenantID))]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey(nameof(ExhibitionID))]
    public virtual Exhibition? Exhibition { get; set; }
}
