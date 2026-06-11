namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد نوع الفعالية المجدولة داخل المعرض.
/// </summary>
public enum EventType
{
    /// <summary>
    /// محاضرة أو عرض معرفي.
    /// </summary>
    Lecture,

    /// <summary>
    /// ورشة عمل تفاعلية.
    /// </summary>
    Workshop,

    /// <summary>
    /// عرض توضيحي لمنتج أو خدمة.
    /// </summary>
    Demo,

    /// <summary>
    /// مراسم أو حفل رسمي.
    /// </summary>
    Ceremony
}
