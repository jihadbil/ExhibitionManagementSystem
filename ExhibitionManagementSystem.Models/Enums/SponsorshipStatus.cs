namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يمثل حالة عقد أو اتفاقية الرعاية.
/// </summary>
public enum SponsorshipStatus
{
    Draft = 1,        // مسودة
    Negotiating = 2,  // قيد التفاوض
    Confirmed = 3,    // مؤكد
    Active = 4,       // نشط ومفعل
    Completed = 5,    // مكتمل
    Cancelled = 6     // ملغي
}
