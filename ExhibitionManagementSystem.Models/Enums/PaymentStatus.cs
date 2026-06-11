namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد حالة الدفعة المالية.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// الدفعة مكتملة ومعتمدة.
    /// </summary>
    Completed,

    /// <summary>
    /// الدفعة قيد الانتظار.
    /// </summary>
    Pending,

    /// <summary>
    /// الدفعة فشلت أو لم تعتمد.
    /// </summary>
    Failed,

    /// <summary>
    /// تم رد قيمة الدفعة.
    /// </summary>
    Refunded
}
