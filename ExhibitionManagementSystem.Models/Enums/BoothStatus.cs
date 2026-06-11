namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد حالة الجناح التشغيلية.
/// </summary>
public enum BoothStatus
{
    /// <summary>
    /// الجناح متاح للحجز.
    /// </summary>
    Available,

    /// <summary>
    /// الجناح محجوز.
    /// </summary>
    Reserved,

    /// <summary>
    /// الجناح جزء من عملية دمج.
    /// </summary>
    Merged,

    /// <summary>
    /// الجناح خارج الخدمة للصيانة.
    /// </summary>
    Maintenance
}
