using System.Windows;
using System.Windows.Media;

namespace ExhibitionManagementSystem.Desktop.Models
{
    public class InsightModel
    {
        public string Message { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info"; // "Positive" | "Warning" | "Critical" | "Info"

        public string IconKind => Severity switch
        {
            "Positive" or "Success" => "CheckCircleOutline",
            "Warning" => "AlertOutline",
            "Critical" or "Danger" => "AlertCircleOutline",
            _ => "InformationOutline"
        };

        public Brush SeverityBrush => Severity switch
        {
            "Positive" or "Success" => Application.Current?.TryFindResource("BrushSuccessText") as Brush ?? Brushes.Green,
            "Warning" => Application.Current?.TryFindResource("BrushWarningText") as Brush ?? Brushes.Orange,
            "Critical" or "Danger" => Application.Current?.TryFindResource("BrushDangerText") as Brush ?? Brushes.Red,
            _ => Application.Current?.TryFindResource("BrushInfoText") as Brush ?? Brushes.Blue
        };

        public Brush SeverityBg => Severity switch
        {
            "Positive" or "Success" => Application.Current?.TryFindResource("BrushSuccessBg") as Brush ?? Brushes.LightGreen,
            "Warning" => Application.Current?.TryFindResource("BrushWarningBg") as Brush ?? Brushes.LightYellow,
            "Critical" or "Danger" => Application.Current?.TryFindResource("BrushDangerBg") as Brush ?? Brushes.MistyRose,
            _ => Application.Current?.TryFindResource("BrushInfoBg") as Brush ?? Brushes.LightBlue
        };
    }
}
