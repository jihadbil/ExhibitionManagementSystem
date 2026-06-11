namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد نوع الجناح المطلوب أو المحجوز.
/// </summary>
public enum BoothType
{
    /// <summary>
    /// مساحة فارغة دون تجهيزات.
    /// </summary>
    SpaceOnly = 1,

    /// <summary>
    /// جناح مجهز مسبقًا.
    /// </summary>
    Equipped = 2,

    /// <summary>
    /// جناح موحد وفق قالب ثابت.
    /// </summary>
    Unified = 3,

    /// <summary>
    /// جناح مخصص حسب طلب العارض.
    /// </summary>
    Custom = 4
}
