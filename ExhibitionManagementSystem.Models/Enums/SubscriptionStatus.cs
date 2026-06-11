namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد حالة اشتراك المستأجر.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// الاشتراك في الفترة التجريبية.
    /// </summary>
    Trial,

    /// <summary>
    /// الاشتراك نشط.
    /// </summary>
    Active,

    /// <summary>
    /// الاشتراك معلق مؤقتًا.
    /// </summary>
    Suspended,

    /// <summary>
    /// انتهت صلاحية الاشتراك.
    /// </summary>
    Expired,

    /// <summary>
    /// تم إلغاء الاشتراك.
    /// </summary>
    Cancelled
}
