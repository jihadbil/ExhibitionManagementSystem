namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد نوع الجناح المطلوب أو المحجوز.
/// </summary>
public enum BoothType
{
    /// <summary>
    /// جناح قياسي.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// جناح زاوية.
    /// </summary>
    Corner = 2,

    /// <summary>
    /// جناح مميز.
    /// </summary>
    Premium = 3,

    /// <summary>
    /// جناح كبار الشخصيات.
    /// </summary>
    VIP = 4,

    /// <summary>
    /// جناح مخصص.
    /// </summary>
    Custom = 5
}
