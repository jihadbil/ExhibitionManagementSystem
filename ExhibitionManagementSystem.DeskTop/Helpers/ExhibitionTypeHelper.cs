namespace ExhibitionManagementSystem.DeskTop.Helpers;

public static class ExhibitionTypeHelper
{
    public static string GetEmoji(string type) => type switch
    {
        "Tech"        => "💻",
        "Medical"     => "🏥",
        "Industrial"  => "🏭",
        "Commercial"  => "🍽️",
        "Educational" => "📚",
        "Automotive"  => "🚗",
        _             => "🏛️"
    };

    public static string GetDisplayName(string type) => type switch
    {
        "Tech"        => "تقنية",
        "Medical"     => "طبية",
        "Industrial"  => "صناعية",
        "Commercial"  => "تجارية",
        "Educational" => "تعليمية",
        "Automotive"  => "سيارات",
        _             => type
    };

    public static string GetGradientKey(string type) => type switch
    {
        "Tech"        => "TechGradientBrush",
        "Medical"     => "MedicalGradientBrush",
        "Industrial"  => "IndustrialGradientBrush",
        "Commercial"  => "CommercialGradientBrush",
        "Educational" => "EducationalGradientBrush",
        "Automotive"  => "AutomotiveGradientBrush",
        _             => "PrimaryGradientBrush"
    };
}
