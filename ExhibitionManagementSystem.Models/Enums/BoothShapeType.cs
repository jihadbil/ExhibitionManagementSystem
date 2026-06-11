namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يحدد شكل الجناح على مخطط القاعة.
/// </summary>
public enum BoothShapeType
{
    /// <summary>
    /// جناح مستطيل عادي.
    /// </summary>
    Rect,

    /// <summary>
    /// جناح مستطيل مع دوران على المخطط.
    /// </summary>
    Rotated,

    /// <summary>
    /// جناح مرسوم كنقاط مضلعة.
    /// </summary>
    Polygon
}
