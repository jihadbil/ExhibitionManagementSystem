namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يمثل نوع المساحة الإعلانية المتاحة في المعرض.
/// </summary>
public enum AdvertisingSpaceType
{
    DigitalScreen = 1,      // شاشات عرض رقمية داخل الصالات
    HangingBanner = 2,      // لافتات معلقة في السقف
    EntranceArch = 3,       // قوس مدخل المعرض
    FloorSticker = 4,       // ملصقات أرضية ثلاثية الأبعاد
    LanyardAndBadge = 5,    // رعاية أشرطة وشارات الدخول
    CatalogFullPage = 6,    // إعلان صفحة كاملة في دليل المعرض
    CatalogHalfPage = 7,    // إعلان نصف صفحة في دليل المعرض
    WebsiteBanner = 8,      // بانر إعلاني على موقع المعرض
    MobileAppSponsor = 9,   // إعلان ممول في تطبيق الهاتف
    ConferenceHall = 10     // رعاية قاعة مؤتمرات / ورشة عمل
}
