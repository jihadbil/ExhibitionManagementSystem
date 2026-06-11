namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد حالة تسجيل الزائر في فعالية مجدولة.
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// الزائر مسجل في الفعالية.
    /// </summary>
    Registered,

    /// <summary>
    /// الزائر حضر الفعالية.
    /// </summary>
    Attended,

    /// <summary>
    /// تم إلغاء التسجيل.
    /// </summary>
    Cancelled,

    /// <summary>
    /// الزائر لم يحضر رغم التسجيل.
    /// </summary>
    NoShow
}
