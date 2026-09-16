namespace ExhibitionManagementSystem.Models.Enums;

/// <summary>
/// يمثل فئة أو مستوى الرعاية.
/// </summary>
public enum SponsorshipLevel
{
    Platinum = 1,     // رعاية بلاتينية
    Gold = 2,         // رعاية ذهبية
    Silver = 3,       // رعاية فضية
    Bronze = 4,       // رعاية برونزية
    TitleSponsor = 5, // الراعي الرئيسي للمؤتمر/المعرض
    Custom = 6        // باقة رعاية مخصصة
}
