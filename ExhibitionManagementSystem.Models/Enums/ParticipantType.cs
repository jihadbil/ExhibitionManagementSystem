namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يمثل نوع المشارك في المعرض المستخدم في توليد وتخصيص بطاقات الدخول (Badges).
/// </summary>
public enum ParticipantType
{
    Visitor = 1,     // زائر عادي
    Exhibitor = 2,   // عارض / ممثل شركة
    Speaker = 3,     // متحدث / محاضر
    VIP = 4,         // ضيف شرف / كبار الشخصيات
    Press = 5,       // صحفي / إعلامي
    Organizer = 6,   // منظم / فريق العمل
    Sponsor = 7      // راعي رسمي
}
