namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد الحالة العامة للمعرض.
/// </summary>
public enum ExhibitionStatus
{
    /// <summary>
    /// المعرض في مرحلة التخطيط.
    /// </summary>
    Planning,

    /// <summary>
    /// المعرض مفتوح أو جارٍ.
    /// </summary>
    Open,

    /// <summary>
    /// المعرض انتهى وأغلق.
    /// </summary>
    Closed,

    /// <summary>
    /// المعرض ملغى.
    /// </summary>
    Cancelled
}
