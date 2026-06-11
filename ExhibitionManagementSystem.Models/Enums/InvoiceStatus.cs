namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد حالة الفاتورة المالية.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// الفاتورة مسودة ولم تصدر بعد.
    /// </summary>
    Draft,

    /// <summary>
    /// الفاتورة صادرة ومطلوبة السداد.
    /// </summary>
    Issued,

    /// <summary>
    /// تم سداد جزء من قيمة الفاتورة.
    /// </summary>
    PartiallyPaid,

    /// <summary>
    /// تم سداد الفاتورة بالكامل.
    /// </summary>
    Paid,

    /// <summary>
    /// الفاتورة ملغاة.
    /// </summary>
    Cancelled
}
