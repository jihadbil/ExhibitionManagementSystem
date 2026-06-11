namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد حالة تذكرة الزائر.
/// </summary>
public enum TicketStatus
{
    /// <summary>
    /// التذكرة نشطة وقابلة للاستخدام.
    /// </summary>
    Active,

    /// <summary>
    /// تم استخدام التذكرة.
    /// </summary>
    Used,

    /// <summary>
    /// التذكرة ملغاة.
    /// </summary>
    Cancelled,

    /// <summary>
    /// انتهت صلاحية التذكرة.
    /// </summary>
    Expired
}
