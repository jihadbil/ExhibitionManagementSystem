namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد طريقة الدفع المستخدمة في تسجيل الدفعات.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// الدفع نقدًا.
    /// </summary>
    Cash,

    /// <summary>
    /// الدفع عبر تحويل مصرفي.
    /// </summary>
    BankTransfer,

    /// <summary>
    /// الدفع بواسطة شيك.
    /// </summary>
    Cheque,

    /// <summary>
    /// الدفع الإلكتروني عبر قناة رقمية.
    /// </summary>
    Online
}
