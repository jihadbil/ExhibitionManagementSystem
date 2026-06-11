namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد حالة حجز الجناح.
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// الحجز قيد المراجعة أو الانتظار.
    /// </summary>
    Pending,

    /// <summary>
    /// الحجز مؤكد.
    /// </summary>
    Confirmed,

    /// <summary>
    /// الحجز ملغى.
    /// </summary>
    Cancelled
}
